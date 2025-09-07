using Unity.Mathematics;
using Unity.Entities;
using InputCamera;
using Unity.Burst;

namespace Enemy.ECS
{
    public partial struct ClusterHighlightingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MousePositionComponent>();
            state.RequireForUpdate<EnemyClusterComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            MousePositionComponent mousePosition = SystemAPI.GetSingleton<MousePositionComponent>();
            state.Dependency = new ClusterHighlightingJob
            { 
                MouseWorldPosition = mousePosition.WorldPosition,
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile]
    public partial struct ClusterHighlightingJob : IJobEntity
    {
        public float3 MouseWorldPosition;

        public void Execute(in EnemyClusterComponent cluster)
        {
            float distX = math.abs(MouseWorldPosition.x - cluster.Position.x);
            float distZ = math.abs(MouseWorldPosition.z - cluster.Position.z);
            if (distX < 1 && distZ < 1)
            {
                //Debug.Log(cluster.Position);
            }
        }
    }
}