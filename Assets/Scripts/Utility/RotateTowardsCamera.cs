using UnityEngine;

public class RotateTowardsCamera : MonoBehaviour
{
    [SerializeField]
    private bool reverseDirection;

    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        Vector3 dir = cam.transform.position - transform.position;
        transform.rotation = Quaternion.LookRotation(dir.normalized * (reverseDirection ? -1 : 1), Vector3.up);
    }
}
