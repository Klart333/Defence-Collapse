using Unity.Entities;

namespace Enemy.ECS.Boss
{
    public struct SpawnBossComponent : IComponentData
    {
        public int SpawnPointIndex;
        public int BossIndex;
    }

    public struct WinOnDeathTag : IComponentData { }
}