using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using System;
using WaveFunctionCollapse;

[InlineEditor]
[CreateAssetMenu(fileName = "Buildable", menuName = "Town/Buildable Corner Data")]
public class BuildableCornerData : SerializedScriptableObject
{
    [Title("Dictionary")]
    public Dictionary<Mesh, BuildableCorners> BuildableDictionary;
        
    public bool IsBuildable(MeshWithRotation meshRot, Vector2Int corner, out bool meshIsBuildable)
    {
        if (meshRot.Mesh == null || !BuildableDictionary.TryGetValue(meshRot.Mesh, out BuildableCorners buildableCorners))
        {
            meshIsBuildable = false;
            return false;
        }

        meshIsBuildable = true;
        Corner rotatedCorner = RotateCorner(meshRot.Rot, corner);
        return buildableCorners.CornerDictionary[rotatedCorner];
    }

    private Corner RotateCorner(int rot, Vector2Int corner)
    {
        float angle = rot * 90 * Mathf.Deg2Rad;
        int x = -Mathf.RoundToInt(corner.x * Mathf.Cos(angle) - corner.y * Mathf.Sin(angle));
        int y = -Mathf.RoundToInt(corner.x * Mathf.Sin(angle) + corner.y * Mathf.Cos(angle));

        return (x, y) switch
        {
            (1, 1) => Corner.TopRight,
            (-1, 1) => Corner.TopLeft,
            (1, -1) => Corner.BottomRight,
            (-1, -1) => Corner.BottomLeft,
            _ => throw new NotImplementedException(),
        };
    }

    public void Clear()
    {
        BuildableDictionary.Clear();   
    }

#if UNITY_EDITOR
    [Button]
    public void Save()
    {
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
    }
    
#endif
}

[System.Serializable]
public class BuildableCorners
{
    [SerializeField]
    public Dictionary<Corner, bool> CornerDictionary = new Dictionary<Corner, bool>() 
    {
        { Corner.TopLeft, false },
        { Corner.TopRight, false },
        { Corner.BottomLeft, false },
        { Corner.BottomRight, false },
    };
}

public enum Corner
{
    TopLeft, 
    TopRight, 
    BottomLeft, 
    BottomRight,
}