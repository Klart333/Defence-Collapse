using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using System;

namespace WaveFunctionCollapse
{
    [InlineEditor]
    [CreateAssetMenu(fileName = "Buildable", menuName = "Town/Buildable Corner Data")]
    public class BuildableCornerData : SerializedScriptableObject
    {
        [Title("Dictionary")]
        public Dictionary<Mesh, BuildableCorners> BuildableDictionary;
        
        [Title("References")]
        [SerializeField]
        private ProtoypeMeshes protoypeMeshes;

        public bool IsCornerBuildable(MeshWithRotation meshRot, int2 corner)
        {
            if (meshRot.MeshIndex == -1 || !BuildableDictionary.TryGetValue(protoypeMeshes.Meshes[meshRot.MeshIndex], out BuildableCorners buildableCorners))
            {
                return false;
            }

            Corner rotatedCorner = RotateCorner(meshRot.Rot, corner);
            return buildableCorners.CornerDictionary[rotatedCorner].Buildable;
        }
        
        public bool IsCornerBuildable(MeshWithRotation meshRot, int2 corner, out bool meshIsBuildable)
        {
            if (meshRot.MeshIndex == -1 || !BuildableDictionary.TryGetValue(protoypeMeshes.Meshes[meshRot.MeshIndex], out BuildableCorners buildableCorners))
            {
                meshIsBuildable = false;
                return false;
            }

            meshIsBuildable = true;
            Corner rotatedCorner = RotateCorner(meshRot.Rot, corner);
            return buildableCorners.CornerDictionary[rotatedCorner].Buildable;
        }
        
        public bool IsCornerBuildable(MeshWithRotation meshRot, int2 corner, out GroundType groundType)
        {
            if (meshRot.MeshIndex == -1 || !BuildableDictionary.TryGetValue(protoypeMeshes.Meshes[meshRot.MeshIndex], out BuildableCorners buildableCorners))
            {
                groundType = default;
                return false;
            }

            Corner rotatedCorner = RotateCorner(meshRot.Rot, corner);
            CornerData cornerData = buildableCorners.CornerDictionary[rotatedCorner];
            groundType = cornerData.GroundType;
            return cornerData.Buildable;
        }

        public static Corner RotateCorner(int rot, int2 corner)
        {
            float angle = rot * 90 * Mathf.Deg2Rad;
            int x = Mathf.RoundToInt(corner.x * Mathf.Cos(angle) - corner.y * Mathf.Sin(angle));
            int y = Mathf.RoundToInt(corner.x * Mathf.Sin(angle) + corner.y * Mathf.Cos(angle));

            return VectorToCorner(x, y);
        }

        public static Corner VectorToCorner(int x, int y)
        {
            return (x, y) switch
            {
                (1, 1) => Corner.TopRight,
                (-1, 1) => Corner.TopLeft,
                (1, -1) => Corner.BottomRight,
                (-1, -1) => Corner.BottomLeft,
                _ => Corner.TopRight,
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
        [InlineProperty]
        public Dictionary<Corner, CornerData> CornerDictionary = new Dictionary<Corner, CornerData>() 
        {
            { Corner.TopLeft, new CornerData() },
            { Corner.TopRight, new CornerData() },
            { Corner.BottomLeft, new CornerData() },
            { Corner.BottomRight, new CornerData() },
        };
    }
        
    [InlineProperty]    
    [System.Serializable]
    public struct CornerData
    {
        [InlineProperty]
        public bool Buildable;
        [InlineProperty, ShowIf(nameof(Buildable))]
        public GroundType GroundType;

        public CornerData(bool buildable)
        {
            Buildable = buildable;
            GroundType = GroundType.Grass;
        }
        
        public static implicit operator bool(CornerData cornerData)
        {
            return cornerData.Buildable;
        }
        
        public static implicit operator CornerData(bool buildable)
        {
            return new CornerData(buildable);
        }
    }

    [Flags]
    public enum GroundType
    {
        Grass = 1 << 0, 
        Crystal = 1 << 1,
        Buildable = Grass | Crystal,
    }

    public enum Corner
    {
        TopLeft, 
        TopRight, 
        BottomLeft, 
        BottomRight,
    }
}
