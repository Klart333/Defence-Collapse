using System.Collections.Generic;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Gameplay.Event;
using UnityEngine;
using Gameplay;
using System;
using Effects;
using Unity.Mathematics;

namespace Buildings.Barricades
{
    public class BarricadeHandler : MonoBehaviour
    {
        public event Action OnAvailableBarricadesChanged;
        public event Action<BarricadeState> OnBarricadeStateCreated; 

        [Title("Setup")]
        [SerializeField]
        private Barricade barricadePrefab;
        
        [Title("Stats")]
        [SerializeField]
        private WallData barricadeData;

        public Dictionary<ChunkIndexEdge, BarricadeState> BarricadeStates { get; } = new Dictionary<ChunkIndexEdge, BarricadeState>();
        public Dictionary<ChunkIndexEdge, Barricade> Barricades { get; } = new Dictionary<ChunkIndexEdge, Barricade>();
        
        public int AvailableBarriers { get; private set; } = 2;
        
        private void OnEnable()
        {
            Events.OnTurnIncreased += OnTurnIncreased;
        }

        private void OnDisable()
        {
            Events.OnTurnIncreased -= OnTurnIncreased;
        }

        private void OnTurnIncreased(int increase, int total)
        {
            foreach (BarricadeState barricadeState in BarricadeStates.Values)
            {
                barricadeState.OnTurnsIncreased(increase);
            }        
        }

        public void PlaceBarricade(ChunkIndexEdge edge)
        {
            BarricadeState state = CreateData(edge);
            BarricadeStates.Add(edge, state);
            
            float angle = edge.EdgeType == EdgeType.West ? 0.0f : 90;
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
            Vector3 position = ChunkWaveUtility.GetPosition(edge);
            
            Barricade spawned = barricadePrefab.GetAtPosAndRot<Barricade>(position, rotation);
            spawned.Place(edge);
            
            Barricades.Add(edge, spawned);
            
            AvailableBarriers = math.max(0, AvailableBarriers - 1);
            OnAvailableBarricadesChanged?.Invoke();
        }

        private BarricadeState CreateData(ChunkIndexEdge edge)
        {
            Stats stats = new Stats(barricadeData.StatGroups);
            stats.Get<MaxHealthStat>().BaseValue *= GameData.BarricadeHealthMultiplier.Value;
            stats.Get<HealingStat>().BaseValue += GameData.BarricadeHealing.Value;
            BarricadeState data = new BarricadeState(this, stats, edge)
            {
                Position = ChunkWaveUtility.GetPosition(edge),
            };
            
            OnBarricadeStateCreated?.Invoke(data);
            
            return data;
        }

        public void DestroyBarricade(ChunkIndexEdge indexEdge)
        {
            BarricadeStates[indexEdge].OnBarricadeDeath();
            BarricadeStates.Remove(indexEdge);

            Barricades[indexEdge].Destroyed();
            Barricades.Remove(indexEdge);

            AvailableBarriers++;
            Events.OnBuiltEdgeDestroyed?.Invoke(indexEdge);
        }

        public void AddAvailableBarricade(int amount)
        {
            AvailableBarriers += amount;
            
            OnAvailableBarricadesChanged?.Invoke();
        }
    }
}