using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using Effects;
using System;

namespace Enemy
{
    
    [InlineEditor, CreateAssetMenu(fileName = "New Enemy Data", menuName = "Enemy/Enemy Data")]
    public class EnemyData : SerializedScriptableObject
    {
        [TitleGroup("Stats")]
        [OdinSerialize, NonSerialized]
        public Stats Stats;

        [Title("Scaling")]
        [SerializeField]
        private float healthScalingMultiplier = 1;

        [Title("Attack")]
        [OdinSerialize, NonSerialized]
        public Attack BaseAttack;

        [Title("Spawning")]
        [SerializeField]
        private bool isBoss = false;

        [SerializeField, ShowIf(nameof(isBoss))]
        private string bossName;

        [SerializeField, ShowIf(nameof(isBoss))]
        private float verticalNameOffset;
        
        [HideIf(nameof(isBoss))]
        public int UnlockedThreshold = 0;

        [HideIf(nameof(isBoss))]
        public int CreditCost = 1;

        [Title("Movement")]
        [SerializeField]
        private int flowFieldImportance = 1;

        [Title("OnDeath", "Money")]
        [SerializeField]
        private float moneyOnDeath = 5;

        [Title("OnDeath", "Explosion")]
        [SerializeField]
        private bool explodeOnDeath = false;

        [ShowIf(nameof(explodeOnDeath))]
        [SerializeField]
        private float explosionSize = 0.5f;

        [Title("OnDeath", "Loot")]
        [SerializeField]
        private bool canDropLoot;

        [ShowIf(nameof(canDropLoot))]
        [SerializeField]
        private float dropLootChance = 0.5f;

        public float HealthScalingMultiplier => healthScalingMultiplier;
        public float VerticalNameOffset => verticalNameOffset;
        public float DropLootChance => dropLootChance;
        public int Importance => flowFieldImportance;
        public bool ExplodeOnDeath => explodeOnDeath;
        public float ExplosionSize => explosionSize;
        public float MoneyOnDeath => moneyOnDeath;
        public bool CanDropLoot => canDropLoot;
        public string BossName => bossName;
        public bool IsBoss => isBoss;

        [TitleGroup("Stats")]
        [Button]
        public void InitStats()
        {
            Stats = new Stats
            {
                HealthDamage = Stats.HealthDamage != null ? new Stat(Stats.HealthDamage.Value) : new Stat(1),
                ArmorDamage = Stats.ArmorDamage != null ? new Stat(Stats.ArmorDamage.Value) : new Stat(1),
                ShieldDamage = Stats.ShieldDamage != null ? new Stat(Stats.ShieldDamage.Value) : new Stat(1),

                AttackSpeed = Stats.AttackSpeed != null ? new Stat(Stats.AttackSpeed.Value) : new Stat(1),
                Range = Stats.Range != null ? new Stat(Stats.Range.Value) : new Stat(1),

                MovementSpeed = Stats.MovementSpeed != null ? new Stat(Stats.MovementSpeed.Value) : new Stat(1),

                CritChance = Stats.CritChance != null ? new Stat(Stats.CritChance.Value) : new Stat(0.01f),
                CritMultiplier = Stats.CritMultiplier != null ? new Stat(Stats.CritMultiplier.Value) : new Stat(2),

                MaxHealth = Stats.MaxHealth != null ? new Stat(Stats.MaxHealth.Value) : new Stat(1),
                MaxArmor = Stats.MaxArmor != null ? new Stat(Stats.MaxArmor.Value) : new Stat(1),
                MaxShield = Stats.MaxShield != null ? new Stat(Stats.MaxShield.Value) : new Stat(1),
                Healing = Stats.Healing != null ? new Stat(Stats.Healing.Value) : new Stat(1),

                Productivity = Stats.Productivity != null ? new Stat(Stats.Productivity.Value) : new Stat(1),
            };
        }
    }
}
