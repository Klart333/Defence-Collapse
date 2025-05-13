using UnityEngine;

namespace Juice
{
    public class DistrictTargetMesh : PooledMonoBehaviour
    {
        public void SetTargetPosition(Vector3 targetPosition)
        {
            transform.LookAt(targetPosition);
        }
    }
}