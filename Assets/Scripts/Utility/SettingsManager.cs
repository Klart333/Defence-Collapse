using Sirenix.OdinInspector;
using UnityEngine;

public class SettingsManager : Singleton<SettingsManager>
{
    [Title("Settings")]
    [SerializeField]
    private int targetFrameRate = 30;

    protected override void Awake()
    {
        base.Awake();

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;
    }

    private void Update()
    {
        if (targetFrameRate != Application.targetFrameRate)
        {
            Application.targetFrameRate = targetFrameRate;
        }
    }
}
