using WaveFunctionCollapse;
using System;
using Effects;
using Gameplay.Buffs;
using Health;
using UnityEngine;

namespace Buildings
{
    public interface IHealthState
    {
        public ChunkIndex Index { get; }
        public HealthComponent Health { get; }
    }

    [Serializable]
    public class WallState : IHealthState, IBuffable
    {
        private BuildingHandler handler;

        public ChunkIndex Index { get; }
        public HealthComponent Health { get; }
        public Stats Stats { get; }
        
        public Vector3 Position { get; set; }

        public WallState(BuildingHandler buildingHandler, Stats stats, ChunkIndex index)
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
                OnBuildingDeath();
            }
        }

        public void TakeDamage(float damage)
        {
            Health.TakeDamage(damage);

            if (!Health.Alive)
            {
                OnBuildingDeath();
            }
        }

        public void OnBuildingDeath()
        {
            handler.BuildingDestroyed(Index);  
        }

        public void OnWaveEnded()
        {
            Health.SetHealthToMax();
        }
    }
}
