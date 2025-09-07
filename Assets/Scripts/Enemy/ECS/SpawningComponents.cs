using Unity.Entities;
using Unity.Mathematics;

namespace Enemy.ECS
{
    public struct SpawnPointComponent : IComponentData
    {
        public float3 Position;
        public Random Random;
        public int Index;
        public bool IsSpawning;
    }

    public struct SpawningComponent : IComponentData
    {
        public Entity SpawnPoint;
        public float3 Position;
        public Random Random;
        
        public int EnemyIndex;
        public float Turns;
        public int Amount;
    }
}