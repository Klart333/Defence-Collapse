using Random = System.Random;
using Buildings.District;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Effects;
using Unity.Entities;
using Effects.ECS;
using Enemy.ECS;
using Juice;

namespace Gameplay
{
    public class GameManager : Singleton<GameManager>
    {
        private SceneTransitionManager sceneTransitionManager; 
            
        public int Seed { get; private set; }
        public bool IsGameOver { get; private set; }
        
        private void OnEnable()
        {
            SetupGame(new Random().Next()); // TODO: Call from proper place, or maybe this is fine?
            
            Events.OnCapitolDestroyed += OnCapitolDestroyed;
            
            GetSceneManager().Forget();
        }

        private void OnDisable()
        {
            Events.OnCapitolDestroyed -= OnCapitolDestroyed;
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
            IsGameOver = true;
            PersistantGameStats.SaveCurrentGameStats();
        }

        public void SetupGame(int seed)
        {
            IsGameOver = false;
            Seed = seed;
            
            PersistantGameStats.CreateNewGameStats(0);
        }

        public static void ResetWorld()
        {
            DOTween.Clear();
            Pool.Clear();
            AttackingSystem.DamageEvent.Clear();
            StopAttackingSystem.KilledIndexes.Clear();
            DamageCallbackHandler.DamageDoneEvent.Clear();
            
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
}