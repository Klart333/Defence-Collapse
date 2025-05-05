using System.Collections.Generic;
using UnityEngine;
using WaveFunctionCollapse;

public interface IBuildable
{
    public MeshWithRotation MeshRot { get; }
    public GameObject gameObject { get; }
    public Transform MeshTransform { get; }
    public MeshRenderer MeshRenderer { get; }
    public ChunkIndex ChunkIndex { get; }
    
    public void Setup(PrototypeData prototypeData, ChunkIndex index, Vector3 scale);
    public void ToggleIsBuildableVisual(bool isQueried, bool showRemoving);
}

