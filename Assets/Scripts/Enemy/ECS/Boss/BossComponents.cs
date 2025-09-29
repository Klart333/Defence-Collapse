using Unity.Entities;

namespace Enemy.ECS.Boss
{
    public struct SpawnBossComponent : IComponentData
    {
        public int SpawnPointIndex;
        public int BossIndex;
        public bool IsFinal;
    }

    public struct WinOnDeathTag : IComponentData { }
}