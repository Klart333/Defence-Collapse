#if UNITY_EDITOR
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using Chunks;

namespace WaveFunctionCollapse
{
    public class GroundBuildableBaker : SerializedMonoBehaviour
    {
        [SerializeField]
        private BuildableCornerData buildableCornerData;
        
        [SerializeField]
        private MeshFilter[] meshFilters;
        
        [SerializeField]
        private Dictionary<Material, GroundType> buildableMaterials;

        [SerializeField]
        private float cellSize = 2.0f;
        
        private readonly int2[] corners =
        {
            new int2(-1,  1),
            new int2( 1,  1),
            new int2(-1, -1),
            new int2( 1, -1),
        };
        
        [Button]
        public void Bake()
        {
            Dictionary<Mesh, BuildableCorners> buildable = new Dictionary<Mesh, BuildableCorners>();

            foreach (MeshFilter meshFilter in meshFilters)
            {
                Vector3 position = meshFilter.transform.position + Vector3.up;

                BuildableCorners buildableCorners = new BuildableCorners{CornerDictionary = new Dictionary<Corner, CornerData>()};
                bool isValid = false;
                for (int i = 0; i < corners.Length; i++)
                {
                    Vector3 pos = position + (new Vector3(corners[i].x, 0, corners[i].y) * cellSize * 0.5f) * 0.8f;
                    Ray ray = new Ray(pos, Vector3.down);
                    Material mat = TreeGrower.GetHitMaterial(ray, out RaycastHit hit);
                    Corner corner = BuildableCornerData.VectorToCorner(corners[i].x, corners[i].y);
                    if (mat != null && buildableMaterials.TryGetValue(mat, out GroundType groundType))
                    {
                        isValid = true;
                        buildableCorners.CornerDictionary.Add(corner, new CornerData
                        {
                            Buildable = true, GroundType = groundType
                        });
                    }
                    else
                    {
                        buildableCorners.CornerDictionary.Add(corner, new CornerData(false));
                    }
                }

                if (isValid)
                {
                    buildable.Add(meshFilter.sharedMesh, buildableCorners);
                }
            }
            
            buildableCornerData.BuildableDictionary = buildable;
            UnityEditor.EditorUtility.SetDirty(buildableCornerData);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(buildableCornerData);
        }
    }
}
#endif
