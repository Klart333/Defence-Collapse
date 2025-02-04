using System;
using UnityEngine;

public class DamageCollider : PooledMonoBehaviour
{
    public event Action OnHit;

    [SerializeField]
    private LayerMask defualtLayerMask;

    public DamageInstance DamageInstance {  get; set; }
    public IAttacker Attacker { get; set; }
    public LayerMask LayerMask { get; set; }
    public bool TriggerDamageDone {  get; set; }

    protected override void OnDisable()
    {
        DamageInstance = null;
        Attacker = null;
        LayerMask = defualtLayerMask;

        transform.localScale = Vector3.one;

        base.OnDisable();
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((LayerMask.value & 1 << other.gameObject.layer) > 0 
            && other.TryGetComponent(out IHealth health) 
            && Attacker is IHealth attackerHealth && health != attackerHealth)
        {
            health.TakeDamage(DamageInstance, out DamageInstance damageDone);
            if (TriggerDamageDone)
            {
                Attacker.OnUnitDoneDamage(damageDone);
            }
            OnHit?.Invoke();
        }
    }
}
