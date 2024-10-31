using UnityEngine;
public class ColliderManager : Singleton<ColliderManager>
{
    [SerializeField]
    private DamageCollider SphereCollider;

    public DamageCollider GetCollider(Vector3 pos, float scale, float lifeTime = 0.1f, LayerMask? layermask = null, bool triggerDamageDone = true)
    {
        DamageCollider collider = SphereCollider.GetAtPosAndRot<DamageCollider>(pos, Quaternion.identity);
        collider.transform.localScale *= scale;
        collider.Delay.Lifeime = lifeTime;
        collider.LayerMask = layermask.GetValueOrDefault(collider.LayerMask);
        collider.TriggerDamageDone = triggerDamageDone;

        return collider;
    }
}
