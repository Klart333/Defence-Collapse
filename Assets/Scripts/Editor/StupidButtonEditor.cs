#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.UI;

namespace EditorFolder
{
    [CustomEditor(typeof(StupidButton), true)]
    [CanEditMultipleObjects]
    public class StupidButtonEditor : SelectableEditor
    {
        SerializedProperty m_OnClickProperty;
        SerializedProperty m_OnHoverEnterProperty;
        SerializedProperty m_OnHoverExitProperty;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_OnClickProperty = serializedObject.FindProperty("m_OnClick");
            m_OnHoverEnterProperty = serializedObject.FindProperty("OnHoverEnter");
            m_OnHoverExitProperty = serializedObject.FindProperty("OnHoverExit");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();

            serializedObject.Update();
            EditorGUILayout.PropertyField(m_OnClickProperty);
            EditorGUILayout.PropertyField(m_OnHoverEnterProperty);
            EditorGUILayout.PropertyField(m_OnHoverExitProperty);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif