#if UNITY_EDITOR
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using Chunks;
using Utility;

namespace WaveFunctionCollapse
{
    public class GroundBuildableBaker : SerializedMonoBehaviour
    {
        [SerializeField]
        private BuildableCornerData buildableCornerData;
        
        [SerializeField]
        private MeshFilter[] meshFilters;
        
        [SerializeField]
        private AtlasAnalyzer atlasAnalyzer;

        [SerializeField]
        private float cellSize = 2.0f;
        
        private readonly int2[] corners =
        {
            new int2(-1, -1),
            new int2(-1, 1),
            new int2(1, -1),
            new int2(1, 1),
        };
        
        private readonly int2[] atlasCorners =
        {
            new int2(0, 0),
            new int2(0, 1),
            new int2(1, 0),
            new int2(1, 1),
        };
        
        [Button]
        public void Bake()
        {
            Dictionary<Mesh, BuildableCorners> buildable = new Dictionary<Mesh, BuildableCorners>();
            List<Color> colors = atlasAnalyzer.GetAtlasColorsBuildable(out List<GroundType> groundTypes);

            foreach (MeshFilter meshFilter in meshFilters)
            {
                BuildableCorners buildableCorners = new BuildableCorners();
                Vector3 position = meshFilter.transform.position + Vector3.up;
                for (int i = 0; i < corners.Length; i++)
                {
                    Vector3 pos = position + (new Vector3(corners[i].x, 0, corners[i].y) * cellSize * 0.5f) * 0.8f;
                    Ray ray = new Ray(pos, Vector3.down);
                    if (!Physics.Raycast(ray, out RaycastHit hit)) continue;

                    Corner corner = BuildableCornerData.VectorToCorner(corners[i].x, corners[i].y);
                    Color color = atlasAnalyzer.Atlas.GetPixel((int)(hit.textureCoord.x * atlasAnalyzer.Atlas.width), (int)(hit.textureCoord.y * atlasAnalyzer.Atlas.height));
                    int index = colors.IndexOf(color);
                    buildableCorners.CornerDictionary[corner] = new CornerData(groundTypes[index]);
                }

                buildable.Add(meshFilter.sharedMesh, buildableCorners);
            }
            
            buildableCornerData.BuildableDictionary = buildable;
            UnityEditor.EditorUtility.SetDirty(buildableCornerData);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(buildableCornerData);
        }
    }
}
#endif
