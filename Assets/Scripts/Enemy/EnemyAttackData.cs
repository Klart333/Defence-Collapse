using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "New Data", menuName = "Enemy/Attack Data")]
public class EnemyAttackData : ScriptableObject
{
    [Title("Stats")]
    public float Damage;

    public float AttackSpeed;

    [Title("Hit Info")]
    public LayerMask LayerMask;

    public float AttackRadius;
}
