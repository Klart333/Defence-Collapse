using UnityEngine;

namespace VFX
{
    public interface IVisualEffectTarget
    {
        public void SetTarget(Vector3 originPosition, Vector3 targetPosition);
    }
}