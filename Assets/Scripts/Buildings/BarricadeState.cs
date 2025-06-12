using Effects;
using Gameplay.Buffs;
using WaveFunctionCollapse;
using Health;
using UnityEngine;

namespace Buildings
{
    [System.Serializable]
    public class BarricadeState : IHealthState, IBuffable
    {
        private BarricadeHandler handler;

        public ChunkIndex Index { get; }
        public HealthComponent Health { get; }
        public Stats Stats { get; }
        
        public Vector3 Position { get; set; }

        public BarricadeState(BarricadeHandler buildingHandler, Stats stats, ChunkIndex index)
        {
            Stats = stats;
            handler = buildingHandler;
            Health = new HealthComponent(Stats);
            Index = index;
        }
    
        public void Update(float dt)
        {
            Health.Update(dt);
        }
        
        public void TakeDamage(DamageInstance damage, out DamageInstance damageDone)
        {
            Health.TakeDamage(damage, out damageDone);

            if (!Health.Alive)
            {
                OnBarricadeDeath();
            }
        }
    
        public void TakeDamage(float damage)
        {
            Health.TakeDamage(damage);

            if (!Health.Alive)
            {
                OnBarricadeDeath();
            }
        }
    
        public void OnBarricadeDeath()
        {
            handler.BarricadeDestroyed(Index);
        }

        public void OnWaveEnded()
        {
            Health.SetHealthToMax();
        }
    }
}