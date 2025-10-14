using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using Variables;
using Effects;
using System;

namespace Enemy
{
    
    [InlineEditor, CreateAssetMenu(fileName = "New Enemy Data", menuName = "Enemy/Enemy Data")]
    public class EnemyData : SerializedScriptableObject
    {
        [Title("Display")]
        [SerializeField]
        private StringReference displayName;
        
        [SerializeField]
        private StringReference description;
        
        [SerializeField]
        private SpriteReference icon; 
        
        [TitleGroup("Stats")]
        [SerializeField]
        private IStatGroup[] statGroups;

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
        private float verticalNameOffset;
        
        [HideIf(nameof(isBoss))]
        public int UnlockedThreshold = 0;

        [HideIf(nameof(isBoss))]
        public int CreditCost = 1;

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
        public string Description => description.Value;
        public float DropLootChance => dropLootChance;
        public bool ExplodeOnDeath => explodeOnDeath;
        public IStatGroup[] StatGroups => statGroups;
        public float ExplosionSize => explosionSize;
        public float MoneyOnDeath => moneyOnDeath;
        public string Name => displayName.Value;
        public bool CanDropLoot => canDropLoot;
        public Sprite Icon => icon.Value;
        public bool IsBoss => isBoss;
    }
}
