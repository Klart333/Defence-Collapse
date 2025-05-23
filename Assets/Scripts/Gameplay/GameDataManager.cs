using Sirenix.OdinInspector;
using Unity.Entities;
using Effects.ECS;
using UnityEngine;

namespace Gameplay
{
    public class GameDataManager : Singleton<GameDataManager>
    {
        [Title("Fire")]
        [SerializeField]
        private FireTickDataComponent defaultFireTickData;
        
        [Title("Poison")]
        [SerializeField]
        private PoisonTickDataComponent defaultPoisonTickData;
        
        private EntityManager entityManager;
        private Entity fireDataEntity;
        private Entity poisonDataEntity;
        
        private FireTickDataComponent fireTickData;
        private PoisonTickDataComponent poisonTickData;
        
        protected override void Awake()
        {
            base.Awake();

            InitailizeGameData();
        }

        private void InitailizeGameData()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            fireDataEntity = entityManager.CreateEntity();
            fireTickData = new FireTickDataComponent
            {
                TickRate = defaultFireTickData.TickRate,
                TickDamage = defaultFireTickData.TickDamage,
            };
            entityManager.AddComponentData(fireDataEntity, fireTickData);
            
            poisonDataEntity = entityManager.CreateEntity();
            poisonTickData = new PoisonTickDataComponent
            {
                TickRate = defaultPoisonTickData.TickRate,
                TickDamage = defaultPoisonTickData.TickDamage,
            };
            entityManager.AddComponentData(poisonDataEntity, poisonTickData);
        }

        public void IncreaseGameData(IComponentData componentData)
        {
            switch (componentData)
            {
                case FireTickDataComponent fireTickDataIncrease:
                    fireTickData.TickDamage += fireTickDataIncrease.TickDamage;
                    fireTickData.TickRate = 1.0f / ((1.0f / fireTickData.TickRate) * fireTickDataIncrease.TickRate);
                    entityManager.SetComponentData(fireDataEntity, fireTickData);
                    break;
                case PoisonTickDataComponent poisonTickDataIncrease:
                    poisonTickData.TickDamage += poisonTickDataIncrease.TickDamage;
                    poisonTickData.TickRate = 1.0f / ((1.0f / poisonTickData.TickRate) * poisonTickDataIncrease.TickRate);
                    entityManager.SetComponentData(poisonDataEntity, poisonTickData);
                    break;
            }
        }
    }
}