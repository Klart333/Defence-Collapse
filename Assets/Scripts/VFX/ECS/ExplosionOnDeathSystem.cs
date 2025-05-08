using Unity.Transforms;
using Unity.Entities;
using Effects.ECS;
using Unity.Burst;

namespace VFX.ECS
{
    [UpdateAfter(typeof(HealthSystem))]
    public partial struct ExplosionOnDeathSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VFXExplosionsSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            VFXExplosionsSingleton vfxExplosionSingleton = SystemAPI.GetSingletonRW<VFXExplosionsSingleton>().ValueRW;
            state.Dependency = new ExplosionOnDeathJob
            {
                ExplosionsManager = vfxExplosionSingleton.Manager,
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
    
    [BurstCompile, WithAll(typeof(DeathTag))]
    public partial struct ExplosionOnDeathJob : IJobEntity
    {
        public VFXManager<VFXExplosionRequest> ExplosionsManager;
        
        public void Execute(in LocalTransform transform, in ExplosionOnDeathComponent explosion)
        {
            ExplosionsManager.AddRequest(new VFXExplosionRequest
            {
                Position = transform.Position,
                Scale = explosion.Size,
            });
        }
    }

    public struct ExplosionOnDeathComponent : IComponentData
    {
        public float Size;
    }
}