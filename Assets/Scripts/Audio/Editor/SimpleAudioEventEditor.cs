using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SimpleAudioEvent))]
public class SimpleAudioEventEditor : Editor
{
    private AudioSource previewSource;

    private SimpleAudioEvent simpleAudioEvent;

    private void OnEnable()
    {
        simpleAudioEvent = (SimpleAudioEvent)target;

        // To be able to preview the sound we need a audio source to play the selected clip. Thus we instantiate a GameObject with a audiosource, we have the flags of not showing it and not saving it for simplicity as it servs no function other than being a audiosource, which does not need showing nor saving. It would be myopic to not do this
        var audioObject = EditorUtility.CreateGameObjectWithHideFlags("Audio Preview", HideFlags.HideAndDontSave, typeof(AudioSource));
        previewSource = audioObject.GetComponent<AudioSource>();
    }

    private void OnDisable()
    {
        DestroyImmediate(previewSource.gameObject); // We kill it to not leave any loose ends
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUI.BeginDisabledGroup(serializedObject.isEditingMultipleObjects); // Disabling the button below if we have multiple objects selected, multiple of the scriptable object 

        if (GUILayout.Button("Preview")) // Creates a button named Preview
        {
            simpleAudioEvent.Play(previewSource);
        }

        EditorGUI.EndDisabledGroup();
    }
}
