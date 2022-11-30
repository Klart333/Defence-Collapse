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

        if (wave.Manual)
        {
            GUILayout.Space(10);

            wave.Auto = GUILayout.Toggle(wave.Auto, "Auto");
            wave.Speed = Mathf.RoundToInt(GUILayout.HorizontalSlider(wave.Speed / 500f, 0.002f, 1.0f) * 500);
            GUILayout.Space(10);
            GUILayout.Label(wave.Speed.ToString());

            GUILayout.Space(20);

            if (GUILayout.Button("Iterate"))
            {
                wave.Iterate();
            }

            if (GUILayout.Button("Propogate"))
            {
                wave.Propagate();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Display Possible Prototypes"))
            {
                wave.DisplayPossiblePrototypes();
            }

            if (GUILayout.Button("Hide Possible Prototypes"))
            {
                wave.HidePossiblePrototypes();
            }



            GUILayout.Space(10);
        }

        base.OnInspectorGUI();
    }
}