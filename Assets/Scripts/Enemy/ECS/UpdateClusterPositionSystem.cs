using Gameplay.Turns.ECS;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using DG.Tweening;
using Effects.ECS;
using Unity.Burst;
using Juice.Ecs;

namespace Enemy.ECS
{
    [BurstCompile, UpdateAfter(typeof(DeathSystem)), UpdateBefore(typeof(FlowMovementSystem))]
    public partial struct UpdateClusterPositionSystem : ISystem
    {
        private BufferLookup<ManagedEntityBuffer> bufferLookup;
        private ComponentLookup<LocalTransform> transformLookup;
        
        private EntityQuery updateEnemiesQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            updateEnemiesQuery = SystemAPI.QueryBuilder().WithAll<TurnIncreaseComponent, UpdateEnemiesTag>().Build();
            state.RequireForUpdate(updateEnemiesQuery);

            transformLookup = SystemAPI.GetComponentLookup<LocalTransform>();
            bufferLookup = SystemAPI.GetBufferLookup<ManagedEntityBuffer>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            TurnIncreaseComponent turnIncrease = updateEnemiesQuery.GetSingleton<TurnIncreaseComponent>();
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            transformLookup.Update(ref state);
            bufferLookup.Update(ref state);

            state.Dependency = new UpdateClusterPositionJob
            {
                TurnIncrease = turnIncrease.TurnIncrease,
                TransformLookup = transformLookup,
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

        } 
    }

    [BurstCompile]
    public partial struct UpdateClusterPositionJob : IJobEntity
    {
        [ReadOnly]
        public BufferLookup<ManagedEntityBuffer> BufferLookup;
        
        [ReadOnly]
        public ComponentLookup<LocalTransform> TransformLookup;
        
        public EntityCommandBuffer.ParallelWriter ECB;
        
        public int TurnIncrease;
        
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, ref FlowFieldComponent flowField, 
            in MovementSpeedComponent movementSpeedComponent, in EnemyClusterComponent clusterComponent)
        {
            flowField.MoveTimer -= TurnIncrease;
            if (flowField.MoveTimer > 0)
            {
                BufferLookup.TryGetBuffer(entity, out DynamicBuffer<ManagedEntityBuffer> buffer);
                float percentage = (movementSpeedComponent.Speed - flowField.MoveTimer) / movementSpeedComponent.Speed;
                float3 axis = math.mul(quaternion.AxisAngle(new float3(0, 1, 0), math.PIHALF), clusterComponent.Facing.XyZ());
                quaternion lookRotation = quaternion.LookRotation(clusterComponent.Facing.XyZ(), new float3(0, 1, 0));
                quaternion targetRotation = math.mul(quaternion.AxisAngle(axis, -math.PIHALF * percentage / 2.0f), lookRotation);

                for (int i = 0; i < buffer.Length; i++)
                {
                    Entity bufferEntity = buffer[i].Entity;
                    LocalTransform transform = TransformLookup[bufferEntity];
                    ECB.AddComponent(sortKey, bufferEntity, new RotationComponent
                    {
                        StartRotation = transform.Rotation,
                        EndRotation = targetRotation,
                        Speed = 2.0f,
                        Ease = Ease.InOutElastic,
                    });
                }
                
                return;
            }
            
            int count = math.max(1, (int)math.ceil(-flowField.MoveTimer / movementSpeedComponent.Speed));
            flowField.MoveTimer += movementSpeedComponent.Speed * count;
            
            ECB.AddComponent(sortKey, entity, new UpdateClusterPositionComponent { Count = count });
        }
    }
}