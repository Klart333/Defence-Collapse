using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using Gameplay;
using UnityEngine;

[InlineEditor, CreateAssetMenu(fileName = "New Tower Data", menuName = "Building/Tower Data")]
public class TowerData : SerializedScriptableObject
{
    [Title("Display")]
    [SerializeField]
    private string districtName = "___ District";

    [SerializeField]
    private Sprite icon;
    
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

    [Title("Range")]
    public PooledMonoBehaviour RangeIndicator;

    [Title("Attack")]
    [OdinSerialize, NonSerialized]
    public Attack BaseAttack;
    
    public string DistrictName => districtName;
    public Sprite Icon => icon;
    public string Description => description;

    [TitleGroup("Stats")]
    [Button]
    public void InitStats()
    {
        Stats = new Stats
        {
            AttackSpeed = Stats.AttackSpeed != null ? new Stat(Stats.AttackSpeed.Value) : new Stat(1),
            DamageMultiplier = Stats.DamageMultiplier != null ? new Stat(Stats.DamageMultiplier.Value) : new Stat(1),
            Range = Stats.Range != null ? new Stat(Stats.Range.Value) : new Stat(1),
            
            MovementSpeed = Stats.MovementSpeed != null ? new Stat(Stats.MovementSpeed.Value) : new Stat(1),
            
            CritChance = Stats.CritChance != null ? new Stat(Stats.CritChance.Value) : new Stat(1),
            CritMultiplier = Stats.CritMultiplier != null ? new Stat(Stats.CritMultiplier.Value) : new Stat(1),
            
            MaxHealth = Stats.MaxHealth != null ? new Stat(Stats.MaxHealth.Value) : new Stat(1),
            MaxArmor = Stats.MaxArmor != null ? new Stat(Stats.MaxArmor.Value) : new Stat(1),
            MaxShield = Stats.MaxShield != null ? new Stat(Stats.MaxShield.Value) : new Stat(1),
            Healing = Stats.Healing != null ? new Stat(Stats.Healing.Value) : new Stat(1),
        };
    }
}

