using UnityEditor;
using UnityEngine;

namespace Variables
{
    [CustomEditor(typeof(ReferenceImage))]
    public class ImageEditor : UnityEditor.UI.ImageEditor
    {
        private SerializedProperty spriteProperty;
        private SerializedProperty colorProperty;

        protected override void OnEnable()
        {
            base.OnEnable();
            
            spriteProperty = serializedObject.FindProperty("spriteReference");
            colorProperty = serializedObject.FindProperty("colorReference");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("References", EditorStyles.whiteBoldLabel);
            EditorGUILayout.PropertyField(spriteProperty, new GUIContent("Sprite Reference"), false, GUILayout.Height(0));
            EditorGUILayout.PropertyField(colorProperty, new GUIContent("Color Reference"), false, GUILayout.Height(0));

            //EditorGUILayout.Space();
            EditorGUILayout.LabelField("Image", EditorStyles.whiteBoldLabel);
            serializedObject.ApplyModifiedProperties();
            
            base.OnInspectorGUI();
        }

    }
}