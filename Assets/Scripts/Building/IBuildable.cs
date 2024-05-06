using System.Collections.Generic;
using UnityEngine;

public interface IBuildable
{
    public GameObject gameObject { get; }
    public void Setup(PrototypeData prototypeData, Vector3 scale);
    public void ToggleIsBuildableVisual(bool value);
    public T GetAtPosAndRot<T>(Vector3 position, Quaternion rotation) where T : PooledMonoBehaviour;
}

