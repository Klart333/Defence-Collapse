using DataStructures.Queue.ECS;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Unity.Jobs;
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
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PendingDamageECBSystem.Singleton>();
            lightningComponentLookup = SystemAPI.GetComponentLookup<LightningComponent>(true);
            transformComponentLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            pendingDamageComponentLookup = SystemAPI.GetComponentLookup<PendingDamageComponent>(true);
            
            EntityQueryBuilder builder = new EntityQueryBuilder(state.WorldUpdateAllocator).WithAll<LightningComponent>();
            state.RequireForUpdate(state.GetEntityQuery(builder));
            
            EntityQueryBuilder builder2 = new EntityQueryBuilder(state.WorldUpdateAllocator).WithAll<PendingDamageComponent>();
            state.RequireForUpdate(state.GetEntityQuery(builder2));
            
            state.RequireForUpdate<SpatialHashMapSingleton>();
            state.RequireForUpdate<VFXChainLightningSingleton>();
            
            lightningRequestQueue = new NativeQueue<VFXChainLightningRequest>(Allocator.Persistent);
            pendingDamageMap = new NativeParallelMultiHashMap<Entity, PendingDamageComponent>(1000, Allocator.Persistent);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            lightningComponentLookup.Update(ref state);
            transformComponentLookup.Update(ref state);
            pendingDamageComponentLookup.Update(ref state);
            
            NativeParallelMultiHashMap<int2, Entity> spatialGrid = SystemAPI.GetSingletonRW<SpatialHashMapSingleton>().ValueRO.Value;

            state.Dependency = new LightningCollisionJob
            {
                LightningLookup = lightningComponentLookup,
                TransformLookup = transformComponentLookup,
                SpatialGrid = spatialGrid.AsReadOnly(),
                LightningRequestQueue = lightningRequestQueue.AsParallelWriter(),
                PendingDamageMap = pendingDamageMap.AsParallelWriter(), 
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
        public NativeParallelMultiHashMap<int2, Entity>.ReadOnly SpatialGrid;

        public NativeParallelMultiHashMap<Entity, PendingDamageComponent>.ParallelWriter PendingDamageMap;

        public NativeQueue<VFXChainLightningRequest>.ParallelWriter LightningRequestQueue;
        
        public void Execute(Entity sourceEntity, in LocalTransform transform, in PendingDamageComponent pendingDamage)
        {
            if (!LightningLookup.TryGetComponent(pendingDamage.SourceEntity, out LightningComponent sourceLightning)) return;

            float3 sourcePosition = transform.Position;
            int2 cellIndex = new int2((int)transform.Position.x, (int)transform.Position.z);
            
            for (int i = 0; i < sourceLightning.Bounces; i++)
            {
                if (!GetClosest(sourceEntity, sourcePosition, cellIndex, out cellIndex, out sourceEntity))
                {
                    return;
                }
                
                float3 targetPosition = TransformLookup.GetRefRO(sourceEntity).ValueRO.Position;
                float3 dir = math.normalize(targetPosition - sourcePosition);
                float zAngle = math.acos(dir.x) * math.TODEGREES;
                LightningRequestQueue.Enqueue(new VFXChainLightningRequest
                {
                    Position = (sourcePosition + targetPosition) / 2.0f,
                    Size =  new float3(math.distance(sourcePosition, targetPosition) * 2, 1, 1),
                    Color = new float3(1, 1, 1),
                    Angle = new float3(0, zAngle, 0),
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
        }

        private bool GetClosest(Entity sourceEntity, float3 sourcePosition, int2 cellIndex, out int2 closestIndex, out Entity closestEntity)
        {
            closestEntity = default;
            float closest = float.MaxValue;
            closestIndex = cellIndex;
            for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            {
                int2 cell = cellIndex + new int2(x, y);
                if (!SpatialGrid.TryGetFirstValue(cell, out Entity enemy, out var iterator)) continue;

                do
                {
                    if (!TransformLookup.TryGetComponent(enemy, out LocalTransform enemyTransform) || enemy == sourceEntity) continue;

                    float distSq = math.distancesq(sourcePosition, enemyTransform.Position);
                    if (distSq > closest) continue;

                    closestEntity = enemy;
                    closest = distSq;
                    closestIndex = cell;
                } while (SpatialGrid.TryGetNextValue(out enemy, ref iterator));
            }
            
            return closestEntity != default;
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
}