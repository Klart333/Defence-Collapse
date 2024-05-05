using Sirenix.OdinInspector;
using UnityEngine;

[InlineEditor, CreateAssetMenu(fileName = "New Archer Data", menuName = "Building/State Data/Archer")]
public class ArcherData : ScriptableObject
{
    [Title("Economy")]
    public int IncomePerHouse = 2;

    [Title("Stats")]
    public float AttackSpeed = 1;
    public float Range = 5;
    public float Damage = 1;

    [Title("Prefabs")]
    public Projectile Arrow;
    public PooledMonoBehaviour RangeIndicator;

    [Title("Growth")]
    public float LevelMultiplier = 1;

    [Title("Health")]
    public int MaxHealth;
}

