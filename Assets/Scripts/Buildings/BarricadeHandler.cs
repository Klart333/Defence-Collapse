using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Pathfinding;
using UnityEngine;
using Enemy.ECS;
using Gameplay;
using UI;

namespace Buildings
{
    public class BarricadeHandler : MonoBehaviour
    {
        public event Action<BarricadeState> OnBarricadeStateCreated; 
        
        [Title("Barricade")]
        [SerializeField]
        private BarricadeGenerator barricadeGenerator;
        
        [Title("Stats")]
        [SerializeField]
        private WallData barricadeData;

        [Title("UI")]
        [SerializeField]
        private UIWallHealth wallHealthPrefab;
        
        [SerializeField]
        private Canvas canvasParent;

        public readonly Dictionary<ChunkIndex, BarricadeState> BarricadeStates = new Dictionary<ChunkIndex, BarricadeState>();
        
        private Dictionary<ChunkIndex, HashSet<Barricade>> barricades = new Dictionary<ChunkIndex, HashSet<Barricade>>();
        private HashSet<ChunkIndex> wallStatesWithHealth = new HashSet<ChunkIndex>();

        private IGameSpeed gameSpeed;
        
        private void Awake()
        {
            GetGameSpeed().Forget();
        }

        private async UniTaskVoid GetGameSpeed()
        {
            gameSpeed = await GameSpeedManager.Get();
        }
        
        private void Update()
        {
            foreach (BarricadeState barricadeState in BarricadeStates.Values)
            {
                barricadeState.Update(Time.deltaTime * gameSpeed.Value);
            }
        }
        
        public void AddBarricade(Barricade barricade)
        {
            List<ChunkIndex> damageIndexes = barricadeGenerator.GetSurroundingMarchedIndexes(barricade.ChunkIndex);

            for (int j = 0; j < damageIndexes.Count; j++)
            {
                ChunkIndex damageIndex = damageIndexes[j];
                if (!BarricadeStates.ContainsKey(damageIndex))
                {
                    BarricadeStates.Add(damageIndex, CreateData(damageIndex));
                }

                if (barricades.TryGetValue(damageIndex, out HashSet<Barricade> barricadeSet))
                {
                    barricadeSet.Add(barricade);
                }
                else
                {
                    barricades.Add(damageIndex, new HashSet<Barricade>(4) { barricade });
                }
            }
        }

        private BarricadeState CreateData(ChunkIndex chunkIndex)
        {
            Stats stats = new Stats(barricadeData.Stats);
            stats.MaxHealth.BaseValue *= GameData.BarricadeHealthMultiplier.Value;
            stats.Healing.BaseValue += GameData.BarricadeHealing.Value;
            BarricadeState data = new BarricadeState(this, stats, chunkIndex)
            {
                Position = barricadeGenerator.GetPos(chunkIndex)
            };
            OnBarricadeStateCreated?.Invoke(data);
            
            return data;
        }
        
        public void RemoveBarricade(Barricade barricade)
        {
            List<ChunkIndex> builtIndexes = barricadeGenerator.GetSurroundingMarchedIndexes(barricade.ChunkIndex);
            foreach (ChunkIndex chunkIndex in builtIndexes)
            {
                if (barricades.TryGetValue(chunkIndex, out HashSet<Barricade> barricadeSet))
                {
                    barricadeSet.Remove(barricade);
                }    
            }
        }
        
        public void BarricadeTakeDamage(ChunkIndex index, float damage, PathIndex pathIndex)
        {
            List<ChunkIndex> damageIndexes = barricadeGenerator.GetSurroundingMarchedIndexes(index);
            damage /= damageIndexes.Count;
            bool didDamage = false;
            for (int i = 0; i < damageIndexes.Count; i++)
            {
                ChunkIndex damageIndex = damageIndexes[i];
                if (!BarricadeStates.TryGetValue(damageIndex, out BarricadeState state)) continue;
                
                float startingHealth = state.Health.CurrentHealth;
                state.TakeDamage(damage);
                didDamage = true;

                DisplayHealth(state, damageIndex, startingHealth);
            }

            if (!didDamage)
            {
                AttackingSystem.DamageEvent.Remove(pathIndex);
                StopAttackingSystem.KilledIndexes.Enqueue(pathIndex);
            }

            void DisplayHealth(BarricadeState state, ChunkIndex damageIndex, float startingHealth)
            {
                if (!state.Health.Alive || !wallStatesWithHealth.Add(damageIndex)) return;
                
                UIWallHealth wallHealth = wallHealthPrefab.Get<UIWallHealth>();
                wallHealth.transform.SetParent(canvasParent.transform, false);
                wallHealth.Setup(state, startingHealth, canvasParent);
                wallHealth.TweenFill();
                wallHealth.OnReturnToPool += OnReturnToPool;

                void OnReturnToPool(PooledMonoBehaviour obj)
                {
                    wallHealth.OnReturnToPool -= OnReturnToPool;
                    wallStatesWithHealth.Remove(damageIndex); 
                }
            }
        }

        public void BarricadeDestroyed(ChunkIndex chunkIndex)
        {
            barricadeGenerator.RevertQuery();

            barricades.Remove(chunkIndex);
            BarricadeStates.Remove(chunkIndex);
            Events.OnBuiltIndexDestroyed?.Invoke(chunkIndex);
        }
    }
}