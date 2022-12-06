using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField]
    private float speed = 1;

    [Header("Less important stats")]
    [SerializeField]
    private float turnSpeed = 0.05f;

    private List<Vector3> path;

    public void SetPath(List<Vector3> path)
    {
        this.path = path;

        StartCoroutine(Move());
    }

    private IEnumerator Move()
    {
        for (int i = 0; i < path.Count - 2; i++)
        {
            float t = 0;
            Vector3 ogPos = transform.position;
            Vector3 targetPos = path[i + 1];

            Quaternion targetRotation = Quaternion.LookRotation((targetPos - ogPos).normalized, Vector3.up);

            while (t <= 1.0f)
            {
                t += Time.deltaTime * speed;

                transform.position = Vector3.Lerp(ogPos, targetPos, t);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed);

                yield return null;
            }
        }

        // Rotation
        float te = 0;
        Quaternion finalRotation = Quaternion.LookRotation((path[path.Count - 1] - transform.position ).normalized, Vector3.up);

        while (te <= 1.0f)
        {
            te += Time.deltaTime * speed;

            transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation, turnSpeed);

            yield return null;
        }

        StartAttacking();
    }

    private void StartAttacking()
    {

    }
}
