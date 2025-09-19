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

        private void OnGUI()
        {
            _smoothingAngle = EditorGUILayout.Slider("Smoothing Angle", _smoothingAngle, 0, MaxSmoothingAngle);
            _channel = (Channel) EditorGUILayout.EnumPopup("Channel", _channel);
            
            if (_channel == Channel.UV8)
            {
                var uvs = new List<Vector4>();
                _sourceMesh.GetUVs(UvChannel, uvs);
                if (uvs.Count > 0)
                {
                    EditorGUILayout.HelpBox($"UV{UvChannel} is busy, it will be overwritten.", MessageType.Warning);
                }

                var boneWeights = new List<BoneWeight>();
                _sourceMesh.GetBoneWeights(boneWeights);

                if (boneWeights.Count > 0)
                {
                    EditorGUILayout.HelpBox(
                        "The mesh seems to be a skinned mesh. Change the Channel to Tangents for correct behavior.",
                        MessageType.Warning
                    );
                }
            }

            if (GUILayout.Button("Compute Smoothed Normals"))
            {
                ComputeSmoothedNormals();
            }
        }

        [MenuItem("Window/Toon RP/Normals Smoothing Utility")]
        private static void OpenWindow()
        {
            NormalsSmoothingUtility window = CreateWindow<NormalsSmoothingUtility>();
            window.titleContent = new GUIContent("Normals Smoothing Utility");
            window.ShowUtility();
        }

        private void ComputeSmoothedNormals()
        {

            Assert.IsNotNull(_sourceMesh);
            Assert.IsTrue(_smoothingAngle > 0f);

            Mesh smoothedMesh = Instantiate(_sourceMesh);
            smoothedMesh.name = _sourceMesh.name + "_SmoothedNormals";
            smoothedMesh.CalculateNormalsAndWriteToChannel(_smoothingAngle, _channel == Channel.UV8 ? UvChannel : null);
            CreateMeshAsset(smoothedMesh);
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

            AssetDatabase.CreateAsset(mesh, path);
        }

        private enum Channel
        {
            UV8,
            Tangents,
        }
    }
}