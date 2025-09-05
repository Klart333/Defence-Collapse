using Unity.Entities;
using Unity.Mathematics;

namespace Enemy.ECS
{
    public struct SpawnPointComponent : IComponentData
    {
        public float3 Position;
        public Random Random;
        public int Index;
    }

    public struct SpawningComponent : IComponentData
    {
        public float3 Position;
        public Random Random;
        
        public int EnemyIndex;
        public float Turns;
        public int Amount;
    }
}