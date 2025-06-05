using Enemy.ECS;
using Juice.Ecs;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Effects.ECS
{
    public class MoneyEntityAuthoring : MonoBehaviour
    {
        private class MoneyEntityAuthoringBaker : Baker<MoneyEntityAuthoring>
        {
            public override void Bake(MoneyEntityAuthoring authoring)
            {
                Entity moneyEntity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                AddComponent(moneyEntity, LocalTransform.FromScale(authoring.transform.localScale.x));
                AddComponent(moneyEntity, new ScaleComponent { TargetScale = 0, Duration = 0.5f, StartScale = authoring.transform.localScale.x });
                //AddComponent(moneyEntity, new RotationComponent {TargetRotation = 6, Duration = 0.5f});
                AddComponent(moneyEntity, new SpeedComponent { Speed = 0.1f });
                AddComponent(moneyEntity, new LifetimeComponent { Lifetime = 0.5f });
                
                AddComponent<RandomComponent>(moneyEntity);
                AddComponent<MovementDirectionComponent>(moneyEntity);
            }
        }
    }
}