using UnityEditor;
using UnityEngine;

namespace Path
{
    [CustomEditor(typeof(FindPath))]
    public class PathEditor : Editor
    {
        private FindPath path;

        private void OnEnable()
        {
            path = target as FindPath;
        }

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Run"))
            {
                path.Run();
            }

            if (GUILayout.Button("Clear"))
            {
                path.Clear();
            }
            
            base.OnInspectorGUI();
        }
    }
}

