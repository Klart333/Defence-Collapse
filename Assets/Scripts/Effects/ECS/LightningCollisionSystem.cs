using Effects.ECS.ECB;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Pathfinding;
using UnityEngine;
using Unity.Jobs;
using Enemy.ECS;
using Gameplay;
using VFX;

namespace Effects.ECS
{
    [BurstCompile, UpdateAfter(typeof(CollisionSystem)), UpdateBefore(typeof(BeforeDamageEffectsECBSystem))]
    public partial struct LightningCollisionSystem : ISystem
    {
        private NativeQueue<VFXChainLightningRequest> lightningRequestQueue;
        
        private ComponentLookup<LightningComponent> lightningComponentLookup;
        private ComponentLookup<LocalTransform> transformComponentLookup;
        private BufferLookup<ManagedEntityBuffer> bufferLookup;
        private BufferLookup<DamageBuffer> damageBufferLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            lightningComponentLookup = SystemAPI.GetComponentLookup<LightningComponent>(true);
            transformComponentLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            bufferLookup = SystemAPI.GetBufferLookup<ManagedEntityBuffer>();
            damageBufferLookup = SystemAPI.GetBufferLookup<DamageBuffer>();
            
            state.RequireForUpdate<BeforeDamageEffectsECBSystem.Singleton>();
            state.RequireForUpdate<VFXChainLightningSingleton>();
            state.RequireForUpdate<SpatialHashMapSingleton>();
            state.RequireForUpdate<LightningComponent>();
            state.RequireForUpdate<PendingDamageTag>();
            
            lightningRequestQueue = new NativeQueue<VFXChainLightningRequest>(Allocator.Persistent);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            NativeParallelHashMap<int2, Entity> spatialGrid = SystemAPI.GetSingletonRW<SpatialHashMapSingleton>().ValueRO.Value;
            var singleton = SystemAPI.GetSingleton<BeforeDamageEffectsECBSystem.Singleton>();
            EntityCommandBuffer ecb = singleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            lightningComponentLookup.Update(ref state);
            transformComponentLookup.Update(ref state);
            damageBufferLookup.Update(ref state);
            bufferLookup.Update(ref state);

            JobHandle jobHandle = new LightningCollisionJob
            {
                LightningRequestQueue = lightningRequestQueue.AsParallelWriter(),
                LightningLookup = lightningComponentLookup,
                TransformLookup = transformComponentLookup,
                DamageBufferLookup = damageBufferLookup,
                SpatialGrid = spatialGrid.AsReadOnly(),
                ECB = ecb.AsParallelWriter(),
                BufferLookup = bufferLookup,
            }.ScheduleParallel(state.Dependency);
            
            VFXChainLightningSingleton vfxLightning = SystemAPI.GetSingletonRW<VFXChainLightningSingleton>().ValueRW;
            state.Dependency = new ApplyVFXJob
            {
                LightningRequestQueue = lightningRequestQueue,
                VFXLightning = vfxLightning,
            }.Schedule(jobHandle);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            lightningRequestQueue.Dispose();
        }
    }

    [BurstCompile(FloatPrecision.Low, FloatMode.Fast), WithAll(typeof(PendingDamageTag))]
    public partial struct LightningCollisionJob : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<LightningComponent> LightningLookup;
        
        [ReadOnly]
        public ComponentLookup<LocalTransform> TransformLookup;

        [ReadOnly]
        public NativeParallelHashMap<int2, Entity>.ReadOnly SpatialGrid;
        
        [ReadOnly]
        public BufferLookup<ManagedEntityBuffer> BufferLookup;
        
        [ReadOnly]
        public BufferLookup<DamageBuffer> DamageBufferLookup;
        
        [WriteOnly]
        public NativeQueue<VFXChainLightningRequest>.ParallelWriter LightningRequestQueue;
        
        public EntityCommandBuffer.ParallelWriter ECB;
        
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in LocalTransform transform)
        {
            DynamicBuffer<DamageBuffer> damageBuffer = DamageBufferLookup[entity];

            for (int i = 0; i < damageBuffer.Length; ++i)
            {
                if (!LightningLookup.TryGetComponent(damageBuffer[i].SourceEntity, out LightningComponent sourceLightning)) return;
                using NativeHashSet<Entity> hitEntities = new NativeHashSet<Entity>(sourceLightning.Bounces, Allocator.Temp);
            
                float3 sourcePosition = transform.Position;
                int2 cellIndex = PathUtility.GetCombinedIndex(sourcePosition.xz);
            
                for (int j = 0; j < sourceLightning.Bounces; j++)
                {
                    hitEntities.Add(entity);
                    if (!GetClosest(hitEntities, cellIndex, out cellIndex, out entity)) return;

                    float3 targetPosition = TransformLookup.GetRefRO(entity).ValueRO.Position;
                    float3 dir = math.normalize(targetPosition - sourcePosition);
                    float zAngle = math.acos(dir.x) * math.TODEGREES;
                    LightningRequestQueue.Enqueue(new VFXChainLightningRequest
                    {
                        Position = (sourcePosition + targetPosition) / 2.0f,
                        Size =  new Vector3(math.distance(sourcePosition, targetPosition) * 2, 1, 1),
                        Color = new Vector3(1, 1, 1),
                        Angle = new Vector3(0, zAngle, 0),
                    });
                
                    sourcePosition = targetPosition;

                    ECB.AppendToBuffer(sortKey, entity, new DamageBuffer
                    {
                        ArmorPenetration = GameDetailsData.LightningArmorPenetration,
                        Damage = sourceLightning.Damage
                    });
                    ECB.AddComponent<PendingDamageTag>(sortKey, entity);
                }
            }
        }

        private bool GetClosest(NativeHashSet<Entity> hitEntities, int2 cellIndex, out int2 closestIndex, out Entity closestEntity)
        {
            closestEntity = default;
            closestIndex = cellIndex;
            for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            {
                int2 cell = new int2(cellIndex.x + x, cellIndex.y + y);
                if (!SpatialGrid.TryGetValue(cell, out Entity cluster) || !BufferLookup.TryGetBuffer(cluster, out DynamicBuffer<ManagedEntityBuffer> buffer)) continue;

                for (int i = 0; i < buffer.Length; i++)
                {
                    Entity enemy = buffer[i].Entity;
                    if (!hitEntities.Add(enemy)) continue;
                
                    closestEntity = enemy;
                    closestIndex = cell;
                    return true;
                }
            }
            
            return false;
        }
    }

    [BurstCompile]
    public struct ApplyVFXJob : IJob
    {
        public NativeQueue<VFXChainLightningRequest> LightningRequestQueue;
        
        public VFXChainLightningSingleton VFXLightning;
        
        public void Execute()
        {
            while (LightningRequestQueue.TryDequeue(out var request))
            {
                VFXLightning.Manager.AddRequest(request);
            }
            
        }
    }
}