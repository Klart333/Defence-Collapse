using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Waves")]
    [SerializeField]
    private Wave[] wavesArray;

    private List<(Vector3, Vector3)> currentSpawnPoints = new List<(Vector3, Vector3)>();
    private Queue<Wave> waves = new Queue<Wave>();

    private Vector3[] path;

    private PathHandler pathHandler;

    private bool inWave = false;

    private void Start()
    {
        for (int i = 0; i < wavesArray.Length; i++)
        {
            waves.Enqueue(wavesArray[i]);
        }

        pathHandler = FindAnyObjectByType<PathHandler>();

        Events.OnWaveClicked += WaveClicked;
    }

    private void WaveClicked()
    { 
        if (inWave) return;

        currentSpawnPoints = pathHandler.GetEnemySpawnPoints();

        if (currentSpawnPoints == null || currentSpawnPoints.Count == 0) 
        {
            // Tell player to connect the castle
            print("// Tell player to connect the castle");
            return;
        }

        StartWave();

        Events.OnWaveStarted.Invoke();
    }

    public async void StartWave()
    {
        if (waves.Count == 0) return;
        inWave = true;

        Wave wave = waves.Dequeue();

        for (int i = 0; i < wave.Bursts.Length; i++)
        {
            SpawnBurst(wave.Bursts[i]);
            await Task.Delay(TimeSpan.FromSeconds(wave.Delays[i]));
        }

        inWave = false;
    }

    private async void SpawnBurst(Burst burst)
    {
        float delay = 1.0f / burst.SpawnRate;
        for (int g = 0; g < burst.Amount; g++)
        {
            await Task.Delay(TimeSpan.FromSeconds(delay));
            SpawnEnemy(burst.Enemy);
        }
    }

    private void SpawnEnemy(EnemyMovement enemy)
    {
        int spawnPoints = UnityEngine.Random.Range(1, currentSpawnPoints.Count + 1);
        for (int i = 0; i < spawnPoints; i++)
        {
            int spawnIndex = UnityEngine.Random.Range(0, currentSpawnPoints.Count);
            EnemyMovement spawnedEnemy = Instantiate(enemy, currentSpawnPoints[spawnIndex].Item1, Quaternion.identity);

            spawnedEnemy.SetPathTarget(currentSpawnPoints[spawnIndex].Item2);
        }
    }
}


