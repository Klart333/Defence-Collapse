using Gameplay.Upgrades;
using UnityEngine;
using System;

namespace Effects
{
    public interface IAttacker
    {
        // Info
        public Stats Stats { get; }
        public CategoryType CategoryType { get; }

        // Position
        public Vector3 AttackPosition { get; set; }
        public Vector3 OriginPosition { get; set; }

        // Attack
        public DamageInstance LastDamageDone { get; }
        public int Key { get; }

        // Events
        public event Action OnAttack;

        // Callbacks
        public void OnUnitDoneDamage(DamageInstance damageInstance);
        public void OnUnitKill();
    }

    public interface IAttackerStatistics
    {
        public event Action OnStatisticsChanged;
        
        public float DamageDone { get; set; }
        public float GoldGained { get; set; }
    }
}