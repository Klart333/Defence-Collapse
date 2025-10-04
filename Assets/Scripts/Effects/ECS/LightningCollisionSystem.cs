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
using VFX;

namespace Effects.ECS
{
    [UpdateAfter(typeof(CollisionSystem)), UpdateBefore(typeof(HealthSystem))]
    public partial struct LightningCollisionSystem : ISystem
    {
        private NativeQueue<VFXChainLightningRequest> lightningRequestQueue;
        private NativeParallelMultiHashMap<Entity, PendingDamageComponent> pendingDamageMap;
        
        private ComponentLookup<LightningComponent> lightningComponentLookup;
        private ComponentLookup<LocalTransform> transformComponentLookup;
        private ComponentLookup<PendingDamageComponent> pendingDamageComponentLookup;
        private BufferLookup<ManagedEntityBuffer> bufferLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PendingDamageECBSystem.Singleton>();
            lightningComponentLookup = SystemAPI.GetComponentLookup<LightningComponent>(true);
            transformComponentLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            pendingDamageComponentLookup = SystemAPI.GetComponentLookup<PendingDamageComponent>(true);
            bufferLookup = SystemAPI.GetBufferLookup<ManagedEntityBuffer>();
                
            EntityQueryBuilder builder = new EntityQueryBuilder(state.WorldUpdateAllocator).WithAll<LightningComponent>();
            state.RequireForUpdate(state.GetEntityQuery(builder));
            
            EntityQueryBuilder builder2 = new EntityQueryBuilder(state.WorldUpdateAllocator).WithAll<PendingDamageComponent>();
            state.RequireForUpdate(state.GetEntityQuery(builder2));
            
            state.RequireForUpdate<SpatialHashMapSingleton>();
            state.RequireForUpdate<VFXChainLightningSingleton>();
            
            lightningRequestQueue = new NativeQueue<VFXChainLightningRequest>(Allocator.Persistent);
            pendingDamageMap = new NativeParallelMultiHashMap<Entity, PendingDamageComponent>(10000, Allocator.Persistent);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            lightningComponentLookup.Update(ref state);
            transformComponentLookup.Update(ref state);
            pendingDamageComponentLookup.Update(ref state);
            bufferLookup.Update(ref state);
            
            NativeParallelHashMap<int2, Entity> spatialGrid = SystemAPI.GetSingletonRW<SpatialHashMapSingleton>().ValueRO.Value;

            state.Dependency = new LightningCollisionJob
            {
                LightningLookup = lightningComponentLookup,
                TransformLookup = transformComponentLookup,
                SpatialGrid = spatialGrid.AsReadOnly(),
                LightningRequestQueue = lightningRequestQueue.AsParallelWriter(),
                PendingDamageMap = pendingDamageMap.AsParallelWriter(), 
                BufferLookup = bufferLookup,
            }.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();

            if (lightningRequestQueue.Count <= 0) return;
            
            VFXChainLightningSingleton vfxLightning = SystemAPI.GetSingletonRW<VFXChainLightningSingleton>().ValueRW;
            var singleton = SystemAPI.GetSingleton<PendingDamageECBSystem.Singleton>();
            EntityCommandBuffer ecb = singleton.CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new ApplyJob
            {
                ECB = ecb,
                PendingDamageMap = pendingDamageMap.AsReadOnly(),
                PendingDamageLookup = pendingDamageComponentLookup,
                LightningRequestQueue = lightningRequestQueue,
                VFXLightning = vfxLightning,
            }.Schedule(state.Dependency);
            state.Dependency.Complete();

            pendingDamageMap.Clear();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            lightningRequestQueue.Dispose();
            pendingDamageMap.Dispose();
        }
    }

    [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
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
        
        public NativeParallelMultiHashMap<Entity, PendingDamageComponent>.ParallelWriter PendingDamageMap;

        public NativeQueue<VFXChainLightningRequest>.ParallelWriter LightningRequestQueue;
        
        public void Execute(Entity sourceEntity, in LocalTransform transform, in PendingDamageComponent pendingDamage)
        {
            if (!LightningLookup.TryGetComponent(pendingDamage.SourceEntity, out LightningComponent sourceLightning)) return;
            NativeHashSet<Entity> hitEntities = new NativeHashSet<Entity>(sourceLightning.Bounces, Allocator.TempJob);
            
            float3 sourcePosition = transform.Position;
            int2 cellIndex = PathUtility.GetCombinedIndex(sourcePosition.xz);
            
            for (int i = 0; i < sourceLightning.Bounces; i++)
            {
                hitEntities.Add(sourceEntity);
                if (!GetClosest(hitEntities, cellIndex, out cellIndex, out sourceEntity))
                {
                    hitEntities.Dispose();
                    return;
                }

                float3 targetPosition = TransformLookup.GetRefRO(sourceEntity).ValueRO.Position;
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

                PendingDamageComponent damage = new PendingDamageComponent
                {
                    ShieldDamage = sourceLightning.Damage * 3,
                    ArmorDamage = sourceLightning.Damage,
                    HealthDamage = sourceLightning.Damage
                };
                PendingDamageMap.Add(sourceEntity, damage);
            }

            hitEntities.Dispose();
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

                Entity enemy = buffer[0].Entity;
                if (!hitEntities.Add(enemy)) continue;
                
                closestEntity = enemy;
                closestIndex = cell;
                return true;
            }
            
            return false;
        }
    }

    [BurstCompile]
    public struct ApplyJob : IJob
    {
        [ReadOnly]
        public NativeParallelMultiHashMap<Entity, PendingDamageComponent>.ReadOnly PendingDamageMap;
        
        [ReadOnly]
        public ComponentLookup<PendingDamageComponent> PendingDamageLookup;
        
        public NativeQueue<VFXChainLightningRequest> LightningRequestQueue;
        
        public VFXChainLightningSingleton VFXLightning;
        
        public EntityCommandBuffer ECB;
        
        public void Execute()
        {
            // Add effects
            while (LightningRequestQueue.TryDequeue(out var request))
            {
                VFXLightning.Manager.AddRequest(request);
            }
            
            // Get all unique keys (entities that were hit)
            NativeArray<Entity> keys = PendingDamageMap.GetKeyArray(Allocator.Temp);
            
            foreach (Entity entity in keys)
            {
                if (!PendingDamageMap.TryGetFirstValue(entity, out PendingDamageComponent damage, out var iterator)) continue;

                if (!PendingDamageLookup.TryGetComponent(entity, out PendingDamageComponent pendingDamage))
                {
                    pendingDamage = new PendingDamageComponent
                    {
                        SourceEntity = damage.SourceEntity
                    };
                }
                
                do 
                {
                    pendingDamage.HealthDamage += damage.HealthDamage;
                    pendingDamage.ArmorDamage += damage.ArmorDamage;
                    pendingDamage.ShieldDamage += damage.ShieldDamage;
                    pendingDamage.IsCrit |= damage.IsCrit;
                } 
                while (PendingDamageMap.TryGetNextValue(out damage, ref iterator));
                    
                ECB.AddComponent(entity, pendingDamage);
            }
            
            keys.Dispose();
        }
    }
    
    /*[BurstCompile]
    public struct MoveByJobChunk : IJobChunk 
    {
        public ComponentTypeHandle<LocalTransform> transformType;
 
        [ReadOnly]
        public ComponentTypeHandle<PendingDamageComponent> velocityType;
 
        public float deltaTime;
 
        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask) 
        { 
            NativeArray<LocalTransform> transforms = chunk.GetNativeArray(ref this.transformType);
            NativeArray<PendingDamageComponent> velocities = chunk.GetNativeArray(ref this.velocityType);
            ChunkEntityEnumerator enumerator = new(useEnabledMask, chunkEnabledMask, chunk.Count);
 
            while (enumerator.NextEntityIndex(out int i)) {
                LocalTransform transform = transforms[i];
                transform.Position += velocities[i].Value * this.deltaTime;
                transforms[i] = transform; // Modify value
            }
        }
    }*/
}