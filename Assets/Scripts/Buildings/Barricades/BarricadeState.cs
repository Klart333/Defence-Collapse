using WaveFunctionCollapse;
using Gameplay.Buffs;
using UnityEngine;
using Effects;
using Health;

namespace Buildings.Barricades
{
    [System.Serializable]
    public class BarricadeState : IBuffable
    {
        private BarricadeHandler handler;

        public Vector3 Position { get; set; }
        public ChunkIndexEdge Index { get; }
        public Stats Stats { get; }

        public BarricadeState(BarricadeHandler buildingHandler, Stats stats, ChunkIndexEdge index)
        {
            Stats = stats;
            handler = buildingHandler;
            Index = index;
        }
    
        public void OnBarricadeDeath()
        {
            
        }

        public void OnTurnsIncreased(int increase)
        {
            
        }
    }
}