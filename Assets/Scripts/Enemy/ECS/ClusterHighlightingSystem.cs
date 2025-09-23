using Gameplay.Turns.ECS;
using Unity.Collections;
using Unity.Entities;
using InputCamera;
using Pathfinding;
using Unity.Burst;

namespace Enemy.ECS
{
    public partial struct ClusterHighlightingSystem : ISystem
    {
        private ComponentLookup<AttackingComponent> attackingLookup;
        private BufferLookup<ManagedEntityBuffer> bufferLookup;
        private NativeReference<PathIndex> lastHightlightPosition;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MousePositionComponent>();
            state.RequireForUpdate<EnemyClusterComponent>();

            attackingLookup = SystemAPI.GetComponentLookup<AttackingComponent>(true);
            bufferLookup = SystemAPI.GetBufferLookup<ManagedEntityBuffer>();
            lastHightlightPosition = new NativeReference<PathIndex>(Allocator.Persistent);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton<TurnIncreaseComponent>(out _))
            {
                lastHightlightPosition.Value = new PathIndex(0, -1);
            }
            
            MousePositionComponent mousePosition = SystemAPI.GetSingleton<MousePositionComponent>();
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            attackingLookup.Update(ref state);
            bufferLookup.Update(ref state);
            
            state.Dependency = new ClusterHighlightingJob
            { 
                MousePathIndex = mousePosition.PathIndex,
                LastIndex = lastHightlightPosition,
                AttackingLookup = attackingLookup,
                ECB = ecb.AsParallelWriter(),
                BufferLookup = bufferLookup,
            }.ScheduleParallel(state.Dependency);
            
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            lastHightlightPosition.Dispose();
        }
    }

    [BurstCompile]
    public partial struct ClusterHighlightingJob : IJobEntity
    {
        [ReadOnly]
        public BufferLookup<ManagedEntityBuffer> BufferLookup;
        
        [ReadOnly]
        public ComponentLookup<AttackingComponent> AttackingLookup;

        [NativeDisableParallelForRestriction]
        public NativeReference<PathIndex> LastIndex;
        
        public EntityCommandBuffer.ParallelWriter ECB;
        
        public PathIndex MousePathIndex;

        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in EnemyClusterComponent cluster, 
            in FlowFieldComponent flowField, in AttackSpeedComponent attackSpeedComponent, in MovementSpeedComponent movementSpeedComponent)
        {
            if (flowField.PathIndex.Equals(LastIndex.Value)) return;
            if (!flowField.PathIndex.Equals(MousePathIndex)) return;
                
            LastIndex.Value = flowField.PathIndex;
            Entity highlightData = ECB.CreateEntity(sortKey);
            ECB.AddComponent(sortKey, highlightData, new HighlightClusterDataComponent
            {
                IsAttacking = AttackingLookup.HasComponent(entity),
                AttackSpeed = attackSpeedComponent.AttackSpeed,
                AttackTimer = attackSpeedComponent.AttackTimer,
                MovementSpeed = movementSpeedComponent.Speed,
                EnemyAmount = BufferLookup[entity].Length,
                PathIndex = flowField.PathIndex,
                MoveTimer = flowField.MoveTimer,
                EnemyType = cluster.EnemyType,
            });
        }
    }

    public struct HighlightClusterDataComponent : IComponentData
    {
        public PathIndex PathIndex;

        public bool IsAttacking;
        public float AttackTimer;
        public float AttackSpeed;
        
        public float MovementSpeed;
        public float MoveTimer;
        
        public int EnemyAmount;
        public int EnemyType;
    }
}