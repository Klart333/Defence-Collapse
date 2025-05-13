using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Editor
{
    // All Overlays must be tagged with the OverlayAttribute
    [Overlay(typeof(SceneView), "Play")]
    // IconAttribute provides a way to define an icon for when an Overlay is in collapsed form. If not provided, the first
    // two letters of the Overlay name will be used.
    [Icon("PlayButton")]
    // Toolbar overlays must inherit `ToolbarOverlay` and implement a parameter-less constructor. The contents of a toolbar
    // are populated with string IDs, which are passed to the base constructor. IDs are defined by
    // EditorToolbarElementAttribute.
    public class PlayModeOverlay : ToolbarOverlay
    {
        PlayModeOverlay() : base(FastPlayButton.id) { }
    }

    // [EditorToolbarElement(Identifier, EditorWindowType)] is used to register toolbar elements for use in ToolbarOverlay
    // implementations.
    [EditorToolbarElement(id, typeof(SceneView))]
    public class FastPlayButton : EditorToolbarButton
    {
        // This ID is used to populate toolbar elements.
        public const string id = "PlayToolbar/FastPlay";

        private static bool wasEnterPlayModeOptionsEnabled;
        private static EnterPlayModeOptions previousOptions;

        public FastPlayButton()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            icon = EditorGUIUtility.IconContent("PlayButton").image as Texture2D;
            text = "Fast Play";
            tooltip = "Enter Playmode without Domain Reload";
            style.backgroundColor = new Color(0f,0.5f, 0f);
            style.color = Color.white;
            clicked += OnClick;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                // Restore previous settings
                EditorSettings.enterPlayModeOptionsEnabled = wasEnterPlayModeOptionsEnabled;
                EditorSettings.enterPlayModeOptions = previousOptions;
            }
        }

        private void OnClick()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
                return;
            }

            // Save current settings
            wasEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            previousOptions = EditorSettings.enterPlayModeOptions;

            // Enable Enter Play Mode Options
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;

            // Start play mode
            EditorApplication.EnterPlaymode();
        }
    }
}
