using UnityEngine;

public class Projectile : PooledMonoBehaviour
{
    private Vector3 lastPos;
    public float Damage { get; set; }

    private void Start()
    {
        lastPos = transform.position;
    }

    private void Update()
    {
        if (lastPos == transform.position)
        {
            return;
        }

        Vector3 dir = (transform.position - lastPos).normalized;
        transform.rotation = Quaternion.LookRotation(dir);

        lastPos = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        other.GetComponentInParent<IHealth>().TakeDamage(Damage);

        gameObject.SetActive(false);
    }
}
