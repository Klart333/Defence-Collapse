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
        private Channel _channel = Channel.UV8;
        
        [SerializeField]
        private string folderFilePath = "Art/Meshes";

        private void OnGUI()
        {
            _smoothingAngle = EditorGUILayout.Slider("Smoothing Angle", _smoothingAngle, 0, MaxSmoothingAngle);
            _channel = (Channel) EditorGUILayout.EnumPopup("Channel", _channel);
            
            if (GUILayout.Button("Compute Smoothed Normals"))
            {
                ComputeSmoothedNormals();
            }
        }

        [MenuItem("Window/Toon RP/Normals Smoothing Utility - ALL")]
        private static void OpenWindow()
        {
            AllNormalsSmoothingUtility window = CreateWindow<AllNormalsSmoothingUtility>();
            window.titleContent = new GUIContent("Normals Smoothing Utility - ALL");
            window.ShowUtility();
        }

        private void ComputeSmoothedNormals()
        {
            var meshes = AssetDatabase.FindAssets("t:mesh");

            foreach (var mesh in meshes)
            {
                Mesh sourceMesh = AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(mesh));
                Assert.IsNotNull(sourceMesh);
                Assert.IsTrue(_smoothingAngle > 0f);

                Mesh smoothedMesh = Instantiate(sourceMesh);
                smoothedMesh.name = sourceMesh.name + "_SmoothedNormals";
                smoothedMesh.CalculateNormalsAndWriteToChannel(_smoothingAngle, _channel == Channel.UV8 ? UvChannel : null);
                CreateMeshAsset(smoothedMesh, AssetDatabase.GUIDToAssetPath(mesh));
            }
            
            AssetDatabase.SaveAssets();
            Close();
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

        private enum Channel
        {
            UV8,
            Tangents,
        }
    }
}