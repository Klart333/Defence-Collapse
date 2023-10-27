using UnityEngine;

[CreateAssetMenu(fileName = "New Archer Data", menuName = "Building/Data/Archer")]
public class ArcherData : ScriptableObject
{
    [Header("Stats")]
    public float AttackSpeed = 1;
    public float Range = 5;
    public float Damage = 1;

    [Header("Prefabs")]
    public Projectile Arrow;
    public PooledMonoBehaviour RangeIndicator;

    [Header("Growth")]
    public float LevelMultiplier = 1;
}
