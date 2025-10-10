// Adapted from https://github.com/Delt06/urp-toon-shader/blob/master/Packages/com.deltation.toon-shader/Assets/DELTation/ToonShader/Editor/NormalsSmoothing/NormalsSmoothingUtility.cs

using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DELTation.ToonRP.Editor.NormalsSmoothing
{
    public class AllNormalsSmoothingUtility : EditorWindow
    {
        public const int UvChannel = 7;
        public const float MaxSmoothingAngle = 180f;
        
        [SerializeField]
        private float _smoothingAngle = MaxSmoothingAngle;

        [SerializeField]
        private NormalsSmoothingUtility.Channel _channel = NormalsSmoothingUtility.Channel.UV8;
        
        [SerializeField]
        private string folderFilePath = "Art/Meshes";
        
        [SerializeField]
        private string createdFolderName = "Smoothed";

        private void OnGUI()
        {
            _smoothingAngle = EditorGUILayout.Slider("Smoothing Angle", _smoothingAngle, 0, MaxSmoothingAngle);
            _channel = (NormalsSmoothingUtility.Channel) EditorGUILayout.EnumPopup("Channel", _channel);
            folderFilePath = EditorGUILayout.TextField("Folder Path", folderFilePath);
            createdFolderName = EditorGUILayout.TextField("Created Folder Name", createdFolderName);
            
            if (GUILayout.Button("Compute Smoothed Normals"))
            {
                ComputeSmoothedNormals();
            }
        }

        [MenuItem("Window/Toon RP/Normals Smoothing Utility - ALL &%w")]
        private static void OpenWindow()
        {
            AllNormalsSmoothingUtility window = CreateWindow<AllNormalsSmoothingUtility>();
            window.titleContent = new GUIContent("Normals Smoothing Utility - ALL");
            window.ShowUtility();
        }

        private void ComputeSmoothedNormals()
        {
            string[] meshesPaths = AssetDatabase.FindAssets("t:mesh", new string[] { folderFilePath });
            
            AssetDatabase.CreateFolder(folderFilePath, createdFolderName);
            AssetDatabase.SaveAssets();
            
            foreach (string meshPath in meshesPaths)
            {
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(meshPath));

                foreach (Object asset in assets)
                {
                    if (asset is not Mesh sourceMesh || sourceMesh.name.EndsWith("_SmoothedNormals")) continue;
                    
                    var smoothedMesh = NormalsSmoothingUtility.ComputeSmoothedNormals(sourceMesh, _smoothingAngle, _channel);
                    string path = $"{folderFilePath}/{createdFolderName}/{smoothedMesh.name}.asset";
                    Debug.Log($"Saving {smoothedMesh.name} at: {path}");
                    CreateMeshAsset(smoothedMesh, path);
                }
                 
            }
            
            AssetDatabase.SaveAssets();
        }
        
        private static void CreateMeshAsset(Mesh mesh, string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                DestroyImmediate(mesh);
                return;
            }

            AssetDatabase.CreateAsset(mesh, assetPath);
        }
    }
}