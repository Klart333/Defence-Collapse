using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Editor
{
    public class MissingMeshFixer : EditorWindow
    {
        private string prefabFolder = "Assets/Prefabs";
        private string meshFolder = "Assets/Meshes";
        private bool includeFBX = true;

        [MenuItem("Tools/Missing Mesh Fixer &%m")] // Ctrl+Alt+M
        public static void ShowWindow()
        {
            GetWindow<MissingMeshFixer>("Missing Mesh Fixer");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Fix Missing MeshFilters", EditorStyles.boldLabel);
            prefabFolder = EditorGUILayout.TextField("Prefab Folder", prefabFolder);
            meshFolder = EditorGUILayout.TextField("Mesh Folder", meshFolder);
            includeFBX = EditorGUILayout.Toggle("Include FBX meshes", includeFBX);

            if (GUILayout.Button("Fix Missing Meshes"))
            {
                FixMissingMeshes(prefabFolder, meshFolder, includeFBX);
            }
        }

        private static void FixMissingMeshes(string prefabFolder, string meshFolder, bool includeFBX)
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolder });
            string meshSearchFilter = includeFBX ? "t:Model t:Mesh" : "t:Mesh";
            string[] meshGuids = AssetDatabase.FindAssets(meshSearchFilter, new[] { meshFolder });

            // Collect all meshes from mesh folder
            List<Mesh> allMeshes = new List<Mesh>();
            foreach (string guid in meshGuids)
            {
                string meshPath = AssetDatabase.GUIDToAssetPath(guid);
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(meshPath);
                foreach (Object obj in assets)
                {
                    if (obj is Mesh mesh)
                        allMeshes.Add(mesh);
                }
            }

            Debug.Log($"Found {allMeshes.Count} meshes in {meshFolder}");

            foreach (string guid in prefabGuids)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                bool modified = false;

                foreach (MeshFilter mf in prefab.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (mf.sharedMesh == null)
                    {
                        string objName = mf.gameObject.name;
                        Mesh bestMatch = FindClosestMeshByName(objName, allMeshes);
                        if (bestMatch != null)
                        {
                            mf.sharedMesh = bestMatch;
                            modified = true;
                            Debug.Log($"Assigned '{bestMatch.name}' to '{objName}' in prefab {prefab.name}");
                        }
                        else
                        {
                            Debug.LogWarning($"No match found for missing mesh on '{objName}' in prefab {prefab.name}");
                        }
                    }
                }

                if (modified)
                {
                    EditorUtility.SetDirty(prefab);
                    PrefabUtility.SavePrefabAsset(prefab);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log("✅ Missing mesh fix complete!");
        }

        private static Mesh FindClosestMeshByName(string targetName, List<Mesh> allMeshes)
        {
            Mesh best = null;
            int bestDistance = int.MaxValue;

            foreach (Mesh mesh in allMeshes)
            {
                string cleanMeshName = CleanName(mesh.name);
                int dist = LevenshteinDistance(targetName.ToLower(), cleanMeshName.ToLower());
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    best = mesh;
                }
            }

            return best;
        }

        private static string CleanName(string name)
        {
            if (name.EndsWith("_SmoothedNormals"))
                name = name.Substring(0, name.Length - "_SmoothedNormals".Length);

            return name.Trim();
        }
        

        // Standard Levenshtein distance implementation
        private static int LevenshteinDistance(string a, string b)
        {
            if (a == b) return 0;
            if (a.Length == 0) return b.Length;
            if (b.Length == 0) return a.Length;

            int[][] matrix = new int[a.Length + 1][];
            for (int index = 0; index < a.Length + 1; index++)
            {
                matrix[index] = new int[b.Length + 1];
            }

            for (int i = 0; i <= a.Length; i++) matrix[i][0] = i;
            for (int j = 0; j <= b.Length; j++) matrix[0][j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = (b[j - 1] == a[i - 1]) ? 0 : 1;
                    matrix[i][j] = Mathf.Min(
                        Mathf.Min(matrix[i - 1][j] + 1, matrix[i][j - 1] + 1),
                        matrix[i - 1][j - 1] + cost);
                }
            }
            return matrix[a.Length][b.Length];
        }
    }
}