using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Gameplay;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Enemy
{
    public class EnemyBossSpawnHandler : MonoBehaviour
    {
        /*
       [Title("References")]
       [SerializeField]
       private EnemySpawnHandler spawnHandler;

       [SerializeField]
       private EnemyBossData bossData;

       [SerializeField]
       private SpawnDataUtility spawnDataUtility;

       private List<Entity> spawnedBossPoints = new List<Entity>();

       private EntityManager entityManager;
       private GameManager gameManager;

       private float spawnFrequency;
       private float spawnTimer;
       private int waveCount;

       private void OnEnable()
       {
           entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
           spawnFrequency = bossData.BossSpawnFrequency;

           Events.OnWaveStarted += OnWaveStarted;

           GetGameManager().Forget();
       }

       private async UniTaskVoid GetGameManager()
       {
           gameManager = await GameManager.Get();
       }

       private void OnDisable()
       {
           Events.OnWaveStarted -= OnWaveStarted;
       }


       private void OnWaveStarted()
       {
           waveCount++;
           spawnTimer++;

           if (spawnTimer >= spawnFrequency)
           {
               spawnTimer -= spawnFrequency;
               spawnFrequency = Mathf.Max(spawnFrequency - bossData.FrequencyDecrease, 1);
               SpawnBosses();
           }
       }*/

        /*private void SpawnBosses()
        {
            List<Vector3> points = new List<Vector3>();
            int totalLevels = 0;
            int amount = 0;
            foreach (List<EnemySpawnPoint> spawnPoints in spawnHandler.SpawnPoints.Values)
            {
                foreach (EnemySpawnPoint spawnPoint in spawnPoints)
                {
                    points.Add(spawnPoint.transform.position);
                    totalLevels += spawnPoint.SpawnLevel;
                    amount++;
                }
            }

            float averageLevel = totalLevels / (float)amount;
            int seed = gameManager.Seed;
            float roundDuration = spawnDataUtility.TargetWaveDuration.Evaluate(waveCount + averageLevel);
            List<EnemyBossData.SpawnPointInfo> pointInfos = bossData.GetBossSpawnPoints(points, totalLevels, seed, roundDuration);

            ComponentType[] componentTypes =
            {
                typeof(LocalTransform),
                typeof(SpawnPointComponent),
            };
            
            for (int i = 0; i < pointInfos.Count; i++)
            {
                if (i >= spawnedBossPoints.Count)
                {
                    spawnedBossPoints.Add(entityManager.CreateEntity(componentTypes));
                }
                
                EnemyBossData.SpawnPointInfo info = pointInfos[i];
                Entity spawnPoint = spawnedBossPoints[i];
                entityManager.SetComponentData(spawnPoint, new SpawnPointComponent
                {
                    Amount = info.Amount,
                    EnemyIndex = info.EnemyIndex,
                    SpawnRate = info.SpawnRate,
                    Timer = info.Timer,
                });
                entityManager.SetComponentData(spawnPoint, LocalTransform.FromPosition(info.Position));
                entityManager.AddComponent<SpawningTag>(spawnPoint);
            }
        }*/
    }
}