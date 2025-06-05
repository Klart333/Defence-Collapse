using WaveFunctionCollapse;
using Health;

namespace Buildings
{
    [System.Serializable]
    public class BarricadeState : IHealthState
    {
        private BarricadeHandler handler;

        public ChunkIndex Index { get; set; }
        public HealthComponent Health { get; set; }

        public BarricadeState(BarricadeHandler buildingHandler, Stats stats, ChunkIndex index)
        {
            handler = buildingHandler;
            Health = new HealthComponent(stats);
            Index = index;
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