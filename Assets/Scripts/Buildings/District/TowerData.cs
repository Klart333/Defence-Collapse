using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Gameplay.Upgrades;
using UnityEngine;
using Variables;
using Gameplay;
using Effects;
using System;

namespace Buildings.District
{
    [InlineEditor, CreateAssetMenu(fileName = "New District Data", menuName = "District/District Data")]
    public class TowerData : SerializedScriptableObject
    {
        [Title("Display")]
        [SerializeField]
        private string districtName = "___ District";

        [SerializeField]
        private StringReference districtNameReference;
        
        [SerializeField]
        private SpriteReference icon;
        
        [SerializeField]
        private SpriteReference iconSmall;

        [SerializeField]
        [TextArea]
        private string description;
        
        [SerializeField]
        private StringReference descriptionReference;

        [SerializeField]
        private DistrictType districtType;

        [TitleGroup("Stats")]
        [OdinSerialize, NonSerialized]
        public Stats Stats;

        public LevelData[] LevelDatas;

        public Sprite[] UpgradeIcons;

        [Title("References")]
        public PooledMonoBehaviour RangeIndicator;

        [Title("Targeting")]
        [SerializeField]
        private bool requireTargeting = true;

        [SerializeField, ShowIf(nameof(requireTargeting))]
        private float attackAngle = 360;
        
        [SerializeField, ShowIf(nameof(requireTargeting))]
        private bool useMeshBasedPlacement = true;
        
        [SerializeField, ShowIf(nameof(requireTargeting))]
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

        [Title("On End Wave")]
        [OdinSerialize]
        private List<IEffect> endWaveEffects = new List<IEffect>();

        public bool UseMeshBasedPlacement => useMeshBasedPlacement;
        public string DistrictName => districtNameReference.Value;
        public string Description => descriptionReference.Value;
        public List<IEffect> EndWaveEffects => endWaveEffects;
        public List<IEffect> CreatedEffects => createdEffects;
        public MeshVariable MeshVariable => meshVariable;
        public DistrictType DistrictType => districtType;
        public bool RequireTargeting => requireTargeting;
        public CategoryType CategoryType => categoryType;
        public bool UseTargetMesh => useTargetMesh;
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

