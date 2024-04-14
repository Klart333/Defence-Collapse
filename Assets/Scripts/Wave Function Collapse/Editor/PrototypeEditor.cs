/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PrototypeInfoCreator))]
public class PrototypeEditor : Editor
{
    private PrototypeInfoCreator prototypeInfo;

    private void OnEnable()
    {
        prototypeInfo = target as PrototypeInfoCreator;
    }

    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Create"))
        {
            prototypeInfo.CreateInfo();
        }

        if (GUILayout.Button("Clear"))
        {
            prototypeInfo.Clear();
        }

        if (prototypeInfo.Debug)
        {
            GUILayout.Space(10);

            if (GUILayout.Button("Display Prototypes"))
            {
                prototypeInfo.DisplayPrototypes();
            }

            if (GUILayout.Button("Stop Displaying Prototypes"))
            {
                prototypeInfo.StopDisplayingPrototypes();
            }
            GUILayout.Space(10);
        }

        base.OnInspectorGUI();
    }
}
*/