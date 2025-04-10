using Cysharp.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Pathfinding.ECS
{
    [AlwaysSynchronizeSystem]
    public partial class UnitPathSystem : SystemBase
    {
        protected override async void OnCreate()
        {
            base.OnCreate();
            Enabled = false;

            await UniTask.WaitUntil(() => PathManager.Instance != null);
            PathManager.Instance.GetPathInformation += () => Enabled = true;
        }

        protected override void OnUpdate()
        {
            new UnitPathJob()
            {
                MovementCosts = PathManager.Instance.MovementCosts,
                GridWidth = PathManager.Instance.GridWidth,
                CellScale = PathManager.Instance.CellScale,
                Units = PathManager.Instance.Units,
            }.Schedule();
            
            Enabled = false;
        }
    }
    
    [BurstCompile, WithAll(typeof(FlowFieldComponent))]
    public partial struct UnitPathJob : IJobEntity
    {
        public NativeArray<short> MovementCosts;
        public NativeArray<short> Units;

        [ReadOnly]
        public float CellScale;

        [ReadOnly]
        public int GridWidth;
        
        [BurstCompile]
        public void Execute(in LocalTransform transform)
        {
            int index = PathManager.GetIndex(transform.Position.x, transform.Position.z, CellScale, GridWidth);
            if (MovementCosts[index] < short.MaxValue)
            {
                MovementCosts[index]++;
            }
            
            if (Units[index] < short.MaxValue)
            {
                Units[index]++;
            }
        }
    }
}