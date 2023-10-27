using UnityEngine;

[CreateAssetMenu(fileName = "New Data", menuName = "Enemy/Attack Data")]
public class EnemyAttackData : ScriptableObject
{
    public float Damage;
    public float AttackSpeed;

    [Header("AoE")]
    public LayerMask LayerMask;
    public bool Splash;
    public float MaxTargets;
    public float AttackRadius;
}
