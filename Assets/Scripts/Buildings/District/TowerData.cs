using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Gameplay.Upgrades;
using UnityEngine;
using Variables;
using Gameplay;
using Effects;
using System;
using WaveFunctionCollapse;

namespace Buildings.District
{
    [InlineEditor, CreateAssetMenu(fileName = "New District Data", menuName = "District/District Data")]
    public class TowerData : SerializedScriptableObject
    {
        [Title("Display")]
        [SerializeField]
        private StringReference districtNameReference;

        [SerializeField]
        private StringReference descriptionReference;
        
        [SerializeField]
        private SpriteReference icon;
        
        [SerializeField]
        private SpriteReference iconSmall;

        [SerializeField]
        private DistrictType districtType;

        [Title("Construction")]
        [SerializeField]
        private bool shouldCombine = true;

        [SerializeField]
        private int districtHeight = 2;
        
        [SerializeField]
        private PrototypeInfoData prototypeInfoData;

        [TitleGroup("Stats")]
        [OdinSerialize, NonSerialized]
        public Stats Stats;

        [TitleGroup("Stats", "Upgrade Stats")]
        [SerializeField]
        private IUpgradeStatEditor[] upgradeStats = Array.Empty<IUpgradeStatEditor>();
        
        [Title("References")]
        public PooledMonoBehaviour RangeIndicator;

        [Title("Targeting")]
        [SerializeField]
        private float attackAngle = 360;
        
        [SerializeField]
        private bool useMeshBasedPlacement = true;
        
        [SerializeField]
        private bool useTargetMesh;

        [SerializeField, ShowIf(nameof(useTargetMesh))]
        private MeshVariable meshVariable;
        
        [Title("Attack")]
        [SerializeField]
        private CategoryType categoryType;
        
        [OdinSerialize, NonSerialized]
        public Attack BaseAttack;

        [Title("On Created")]
        [OdinSerialize]
        private List<IEffect> createdEffects = new List<IEffect>();

        [Title("On Turn Complete")]
        [OdinSerialize]
        private List<IEffect> endWaveEffects = new List<IEffect>();

        public PrototypeInfoData PrototypeInfoData => prototypeInfoData;
        public bool UseMeshBasedPlacement => useMeshBasedPlacement;
        public string DistrictName => districtNameReference.Value;
        public IUpgradeStatEditor[] UpgradeStats => upgradeStats;
        public string Description => descriptionReference.Value;
        public List<IEffect> EndWaveEffects => endWaveEffects;
        public List<IEffect> CreatedEffects => createdEffects;
        public MeshVariable MeshVariable => meshVariable;
        public DistrictType DistrictType => districtType;
        public CategoryType CategoryType => categoryType;
        public int DistrictHeight => districtHeight;
        public bool UseTargetMesh => useTargetMesh;
        public bool ShouldCombine => shouldCombine;
        public Sprite IconSmall => iconSmall.Value;
        public float AttackAngle => attackAngle;
        public Sprite Icon => icon.Value;

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

