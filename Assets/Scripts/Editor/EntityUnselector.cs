namespace Editor
{
    using UnityEditor;

    [InitializeOnLoad]
    public static class EntityUnselector
    {
        static EntityUnselector()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Run only when exiting play mode (returning to Edit Mode)
            if (state is PlayModeStateChange.ExitingPlayMode or PlayModeStateChange.EnteredEditMode)
            {
                Selection.activeObject = null;
            }
        }
    }

}