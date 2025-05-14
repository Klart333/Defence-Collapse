using Gameplay.Money;
using Random = Unity.Mathematics.Random;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using UnityEngine;

namespace Effects.ECS
{
    [UpdateAfter(typeof(HealthSystem))]
    public partial struct MoneyOnDeathSystem : ISystem
    {
        private EntityQuery moneyOnDeathQuery;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MoneyToAddComponent>();
            state.RequireForUpdate<MoneyPrefabComponent>();
            
            EntityQueryBuilder builder = new EntityQueryBuilder(state.WorldUpdateAllocator).WithAll<DeathTag, MoneyOnDeathComponent>();
            moneyOnDeathQuery = state.GetEntityQuery(builder);
            state.RequireForUpdate(moneyOnDeathQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity moneyPrefab = SystemAPI.GetSingleton<MoneyPrefabComponent>().MoneyPrefab;
            float scale = state.EntityManager.GetComponentData<LocalTransform>(moneyPrefab).Scale; 
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            NativeReference<float> moneyArray = new NativeReference<float>(0, Allocator.TempJob);

            state.Dependency = new MoneyOnDeathJob
            {
                MoneyPrefab = moneyPrefab,
                ECB = ecb,
                Scale = scale,
                BaseSeed = UnityEngine.Random.Range(0, 100000),
                TotalMoney = moneyArray,
            }.Schedule(state.Dependency);
            
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            SystemAPI.SetSingleton(new MoneyToAddComponent { Money = moneyArray.Value });
            moneyArray.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            
        }
    }

    [BurstCompile, WithAll(typeof(DeathTag))]
    public partial struct MoneyOnDeathJob : IJobEntity
    {
        public NativeReference<float> TotalMoney;
        public Entity MoneyPrefab;
        public float Scale;
        public int BaseSeed;
        
        public EntityCommandBuffer ECB;
        
        public void Execute([EntityIndexInChunk] int sortKey, in LocalTransform localTransform, in MoneyOnDeathComponent money)
        {
            TotalMoney.Value += money.Amount;

            for (int i = 0; i < money.Amount; i++)
            {
                Entity spawnedMoney = ECB.Instantiate(MoneyPrefab);
                Random random = Random.CreateFromIndex((uint)(BaseSeed + sortKey + i));
                ECB.SetComponent(spawnedMoney, new RandomComponent { Random = random});
                ECB.SetComponent(spawnedMoney, new MovementDirectionComponent { Direction = random.NextFloat3Direction() });
                ECB.SetComponent(spawnedMoney, new LocalTransform
                {
                    Position = localTransform.Position + random.NextFloat3(new float3(-1, 1f, -1), new float3(1, 2f, 1)) * Scale * 5,
                    Rotation = random.NextQuaternionRotation(),
                    Scale = Scale
                });
            }
        }
    }
}