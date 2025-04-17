using DataStructures.Queue.ECS;
using Random = System.Random;
using Buildings.District;
using Unity.Entities;
using DG.Tweening;
using Effects.ECS;

namespace Gameplay
{
    public class GameManager : Singleton<GameManager>
    {
        public int Seed { get; private set; }
        public bool IsGameOver { get; private set; }
        
        private void OnEnable()
        {
#if UNITY_EDITOR
            SetupGame(new Random().Next());  
#endif   
            
            Events.OnCapitolDestroyed += OnCapitolDestroyed;
        }

        private void OnDisable()
        {
            Events.OnCapitolDestroyed -= OnCapitolDestroyed;
        }

        private void OnCapitolDestroyed(DistrictData destroyedDistrict)
        {
            IsGameOver = true;
        }

        public void SetupGame(int seed)
        {
            IsGameOver = false;
            Seed = seed;
        }

        public void ResetWorld()
        {
            Pool.Clear();
            AttackingSystem.DamageEvent.Clear();
            StopAttackingSystem.KilledIndexes.Clear();
            CollisionSystem.DamageDoneEvent.Clear();
            
            World defaultWorld = World.DefaultGameObjectInjectionWorld;
            defaultWorld.EntityManager.CompleteAllTrackedJobs();
            foreach (ComponentSystemBase system in defaultWorld.Systems)
            {
                system.Enabled = false;
            }
            defaultWorld.Dispose();
            DefaultWorldInitialization.Initialize("Default World");
            if (!ScriptBehaviourUpdateOrder.IsWorldInCurrentPlayerLoop(World.DefaultGameObjectInjectionWorld))
            {
                ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(World.DefaultGameObjectInjectionWorld);
            }
            
            Events.OnGameReset?.Invoke();
        }
    }
}