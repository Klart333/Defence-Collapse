using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WaveFunction))]
public class WaveEditor : Editor
{
    private WaveFunction wave;

    private void OnEnable()
    {
        wave = target as WaveFunction;
    }

    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Run"))
        {
            wave.Run();
        }

        if (GUILayout.Button("Clear"))
        {
            wave.Clear();
        }

        base.OnInspectorGUI();
    }
}