using UnityEngine;

[CreateAssetMenu(fileName = "New Data", menuName = "Fighter/Attack Data")]
public class FighterAttackData : ScriptableObject
{
    public float Damage;
    public float AttackSpeed;

    [Header("AoE")]
    public LayerMask LayerMask;
    public bool Splash;
    public float MaxTargets;
    public float AttackRadius;
}
