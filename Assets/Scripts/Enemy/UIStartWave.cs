using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIStartWave : MonoBehaviour
{
    public void StartWave()
    {
        Events.OnWaveClicked?.Invoke();
    }
}
