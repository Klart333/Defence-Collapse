using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using Gameplay;
using System;
using Effects;
using Juice;
using Variables;

namespace Buildings.District
{
    [InlineEditor, CreateAssetMenu(fileName = "New District Data", menuName = "District/District Data")]
    public class TowerData : SerializedScriptableObject
    {
        [Title("Display")]
        [SerializeField]
        private string districtName = "___ District";

        [SerializeField]
        private SpriteReference icon;
        
        [SerializeField]
        private SpriteReference iconSmall;

        [SerializeField]
        [TextArea]
        private string description;

        [SerializeField]
        private DistrictType districtType;

        [TitleGroup("Stats")]
        [OdinSerialize, NonSerialized]
        public Stats Stats;

        public LevelData[] LevelDatas;

        public Sprite[] UpgradeIcons;

        [Title("References")]
        public PooledMonoBehaviour RangeIndicator;

        [SerializeField]
        private DistrictTargetMesh districtTargetMesh;

        [Title("Attack")]
        [OdinSerialize, NonSerialized]
        public Attack BaseAttack;

        [Title("On Created")]
        [OdinSerialize]
        private List<IEffect> createdEffects = new List<IEffect>();

        [Title("On End Wave")]
        [OdinSerialize]
        private List<IEffect> endWaveEffects = new List<IEffect>();

        public DistrictTargetMesh DistrictTargetMesh => districtTargetMesh;
        public List<IEffect> EndWaveEffects => endWaveEffects;
        public List<IEffect> CreatedEffects => createdEffects;
        public DistrictType DistrictType => districtType;
        public string DistrictName => districtName;
        public string Description => description;
        public Sprite IconSmall => iconSmall.Value;
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

