using UnityEngine;

public class AttackVisualEffect : PooledMonoBehaviour
{
    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        Reset();
    }

    private void Reset()
    {
        transform.localScale = originalScale;
    }

    public AttackVisualEffect Spawn(Vector3 pos, Quaternion rot, float scale, float lifetime = 1)
    {
        AttackVisualEffect gm = GetAtPosAndRot<AttackVisualEffect>(pos, rot);
        gm.transform.localScale = gm.transform.localScale * scale;
        gm.Delay.Lifeime = lifetime;

        return gm;
    }
}