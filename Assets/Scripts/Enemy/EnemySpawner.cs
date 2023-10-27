using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Waves")]
    [SerializeField]
    private Wave[] wavesArray;

    private Queue<Wave> waves = new Queue<Wave>();
    private WaveFunction waveFunction;

    private Vector3[] path;

    private bool inWave = false;

    private void Start()
    {
        for (int i = 0; i < wavesArray.Length; i++)
        {
            waves.Enqueue(wavesArray[i]);
        }

        waveFunction = FindObjectOfType<WaveFunction>();
        waveFunction.OnMapGenerated += Wave_OnMapGenerated;

        Events.OnWaveClicked += WaveClicked;
    }

    private void WaveClicked()
    {
        if (inWave)
        {
            return;
        }

        StartCoroutine(StartWave());

        Events.OnWaveStarted.Invoke();
    }

    private void Wave_OnMapGenerated()
    {
        var cells = waveFunction.GetEnemyPath();
        path = new Vector3[cells.Count];
        for (int i = 0; i < cells.Count; i++)
        {
            path[i] = cells[i].Position;
        }
    }

    public IEnumerator StartWave()
    {
        if (waves.Count == 0)
        {
            yield break;
        }
        inWave = true;

        Wave wave = waves.Dequeue();

        for (int i = 0; i < wave.Bursts.Length; i++)
        {
            StartCoroutine(SpawnBurst(wave.Bursts[i]));
            yield return new WaitForSeconds(wave.Delays[i]);
        }

        inWave = false;
    }

    private IEnumerator SpawnBurst(Burst burst)
    {
        float delay = 1.0f / burst.SpawnRate;
        for (int g = 0; g < burst.Amount; g++)
        {
            SpawnEnemy(burst.Enemy);
            yield return new WaitForSeconds(delay);
        }
    }

    private void SpawnEnemy(EnemyMovement enemy)
    {
        var spawnedEnemy = Instantiate(enemy, path[0], Quaternion.identity);

        spawnedEnemy.UpdatePath();
    }
}


