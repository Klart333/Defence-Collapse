using Random = System.Random;
using Buildings.District;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Effects;
using Unity.Entities;
using Effects.ECS;
using Enemy.ECS;
using Gameplay.Event;
using Juice;

namespace Gameplay
{
    public class GameManager : Singleton<GameManager>
    {
        private SceneTransitionManager sceneTransitionManager; 
        
        private EntityManager entityManager;
            
        public uint Seed { get; private set; }
        public bool IsGameOver { get; private set; }
        
        private void OnEnable()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            SetupGame((uint)UnityEngine.Random.Range(1, 100_000_000)); // TODO: Call from proper place, or maybe this is fine?
            
            Events.OnCapitolDestroyed += OnCapitolDestroyed;
            Events.OnFinalBossDeafeted += SetGameOver;
            
            GetSceneManager().Forget();
        }

        private void OnDisable()
        {
            Events.OnCapitolDestroyed -= OnCapitolDestroyed;
            Events.OnFinalBossDeafeted -= SetGameOver;
            sceneTransitionManager.OnSceneBeginChange -= OnBeginSceneChange;
            Pool.Clear();
        }
        
        private async UniTaskVoid GetSceneManager()
        {
            sceneTransitionManager = await SceneTransitionManager.Get();
            sceneTransitionManager.OnSceneBeginChange += OnBeginSceneChange;
        }

        private void OnBeginSceneChange()
        {
            IsGameOver = true;
            ResetWorld();
        }
        
        private void OnCapitolDestroyed(DistrictData destroyedDistrict)
        {
            SetGameOver();
        }
        
        private void SetGameOver()
        {
            IsGameOver = true;
            PersistantGameStats.SaveCurrentGameStats();
        }
        
        public void SetupGame(uint seed)
        {
            IsGameOver = false;
            Seed = seed;
            
            entityManager.AddComponentData(entityManager.CreateEntity(), new RandomSeedComponent { Seed = seed });
            
            PersistantGameStats.CreateNewGameStats(0);
        }

        public static void ResetWorld()
        {
            DOTween.Clear();
            Pool.Clear();
            AttackingSystem.DamageEvent.Clear();
            StopAttackingSystem.KilledIndexes.Clear();
            DamageCallbackHandler.DamageDoneEvent.Clear();
            EffectEntityPrefabs.Clear();
            
            World defaultWorld = World.DefaultGameObjectInjectionWorld;
            if (defaultWorld != null)
            {
                defaultWorld.EntityManager.CompleteAllTrackedJobs();
                foreach (ComponentSystemBase system in defaultWorld.Systems)
                {
                    system.Enabled = false;
                }
                defaultWorld.Dispose();
            }
           
            DefaultWorldInitialization.Initialize("Default World");
            if (!ScriptBehaviourUpdateOrder.IsWorldInCurrentPlayerLoop(World.DefaultGameObjectInjectionWorld))
            {
                ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(World.DefaultGameObjectInjectionWorld);
            }
            
            Events.OnGameReset?.Invoke();
        }
    }

    public struct RandomSeedComponent : IComponentData
    {
        public uint Seed;
    }
}