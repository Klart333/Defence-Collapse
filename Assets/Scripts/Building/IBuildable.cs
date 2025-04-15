using System.Collections.Generic;
using UnityEngine;
using WaveFunctionCollapse;

public interface IBuildable
{
    public int Importance { get; }
    public MeshWithRotation MeshRot { get; }
    public GameObject gameObject { get; }
    public Transform MeshTransform { get; }
    public ChunkIndex ChunkIndex { get; }
    
    public void Setup(PrototypeData prototypeData, Vector3 scale);
    public void ToggleIsBuildableVisual(bool value);
}

