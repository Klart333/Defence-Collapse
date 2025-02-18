using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIStartWave : MonoBehaviour
{
    public void StartWave()
    {
        Events.OnWaveStarted?.Invoke(); // NEEDS TO CHECK IF A PORTAL IS UNLOCKED
    }
}
