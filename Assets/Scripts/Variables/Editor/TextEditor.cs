using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;

namespace Variables
{
    [CustomEditor(typeof(ReferenceText))]
    public class TextEditor : TMP_EditorPanelUI
    {
        private SerializedProperty fontProperty;
        private SerializedProperty colorProperty;

        protected override void OnEnable()
        {
            base.OnEnable();
            
            fontProperty = serializedObject.FindProperty("fontReference");
            colorProperty = serializedObject.FindProperty("colorReference");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("References", EditorStyles.whiteBoldLabel);
            EditorGUILayout.PropertyField(fontProperty, new GUIContent("Font Reference"), false, GUILayout.Height(0));
            EditorGUILayout.PropertyField(colorProperty, new GUIContent("Color Reference"), false, GUILayout.Height(0));

            serializedObject.ApplyModifiedProperties();
            
            base.OnInspectorGUI();
        }
    }
}