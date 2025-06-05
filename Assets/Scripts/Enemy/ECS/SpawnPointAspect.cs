using Effects.ECS;
using Enemy;
using Unity.Entities;
using Unity.Transforms;

namespace Enemy.ECS
{
    public readonly partial struct SpawnPointAspect : IAspect
    {
        public readonly RefRW<SpawnPointComponent> SpawnPointComponent;
        public readonly RefRW<RandomComponent> RandomComponent;
        public readonly RefRO<LocalTransform> Transform;
        public readonly RefRO<SpawningTag> SpawningTag;
    }
}