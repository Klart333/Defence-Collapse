using UnityEngine;
using UnityEditor;
using System.IO;
using DELTation.ToonRP.Editor.NormalsSmoothing;

namespace Editor
{
    public class NormalsSmoothingPostprocessor : AssetPostprocessor
    {
        // Define your smoothing angle (you can make this configurable later)
        private const float SmoothingAngle = 180f;

        // Called automatically by Unity after any model (.fbx, .obj, etc.) is imported
        void OnPostprocessModel(GameObject importedModel)
        {
            return;
            string assetPath = assetImporter.assetPath;

            // Get all MeshFilters in the imported model hierarchy
            MeshFilter[] meshFilters = importedModel.GetComponentsInChildren<MeshFilter>();
            if (meshFilters == null || meshFilters.Length == 0)
                return;

            foreach (var meshFilter in meshFilters)
            {
                Mesh mesh = meshFilter.sharedMesh;
                if (mesh == null) continue;

                // Skip meshes that already have "_SmoothedNormals" in the name to avoid recursion
                if (mesh.name.Contains("_SmoothedNormals"))
                    continue;

                // Compute smoothed normals into UV8
                Mesh smoothedMesh = NormalsSmoothingUtility.ComputeSmoothedNormals(mesh, SmoothingAngle);

                // Save new mesh asset beside the original
                string directory = Path.GetDirectoryName(assetPath);
                string meshName = Path.GetFileNameWithoutExtension(mesh.name);
                string smoothedPath = Path.Combine(directory, meshName + "_SmoothedNormals.asset");
                smoothedPath = AssetDatabase.GenerateUniqueAssetPath(smoothedPath);

                AssetDatabase.CreateAsset(smoothedMesh, smoothedPath);
                AssetDatabase.SaveAssets();

                Debug.Log($"Generated smoothed mesh with UV8 normals at: {smoothedPath}");
            }
        }
    }
}
