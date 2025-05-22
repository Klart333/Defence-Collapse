using Effects.ECS;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

namespace Gameplay
{
    public class GameDataManager : Singleton<GameDataManager>
    {
        [Title("Fire")]
        [SerializeField]
        private float tickDamage = 20;
        
        [SerializeField]
        private float tickRate = 0.4f;
        
        private EntityManager entityManager;
        private Entity fireDataEntity;
        
        protected override void Awake()
        {
            base.Awake();

            InitailizeGameData();
        }

        private void InitailizeGameData()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            fireDataEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(fireDataEntity, new FireTickDataComponent
            {
                TickRate = tickRate,
                TickDamage = tickDamage
            });
        }
    }
}