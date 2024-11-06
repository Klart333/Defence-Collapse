using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[CreateAssetMenu(fileName = "Buildable", menuName = "Ground Cell Buildable Utility")]
public class GroundCellBuildableUtility : SerializedScriptableObject
{
    [Title("Dictionary")]
    [SerializeField]
    private Dictionary<Mesh, BuildableCorners> BuildableDictionary = new();
        
    public bool IsBuildable(MeshWithRotation meshRot, Vector2 corner)
    {
        if (meshRot.Mesh == null)
        {
            return false;
        }

        Corner rotatedCorner = RotateCorner(meshRot.Rot, corner);
        return BuildableDictionary[meshRot.Mesh].CornerDictionary[rotatedCorner];
    }

    private Corner RotateCorner(int rot, Vector2 corner)
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