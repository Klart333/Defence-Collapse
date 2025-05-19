using System;
using System.Collections.Generic;
using System.Linq;
using Buildings;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using Enemy;
using Sirenix.OdinInspector;

namespace Pathfinding
{
    public class FlowFieldVisualizer : MonoBehaviour
    {
        [Title("References")]
        [SerializeField]
        private LineVisualizer linePrefab;

        [SerializeField]
        private EnemySpawnHandler enemySpawner;
        
        [Title("Settings")]
        [SerializeField]
        private float updateInterval = 0.5f;
        
        private readonly List<LineVisualizer> spawnedLines = new List<LineVisualizer>();
        
        private PathManager pathManager;

        private bool isDisplaying;
        private float updateTimer;
        private int updateIndex;
        
        private bool Displaying => isDisplaying || PathPlacer.Displaying;
        
        private void OnEnable()
        {
            GetPathManager().Forget();
        }

        private async UniTaskVoid GetPathManager()
        {
            pathManager = await PathManager.Get();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                isDisplaying = !isDisplaying;
            }
            
            switch (Displaying)
            {
                case true when spawnedLines.Count <= 0:
                    DisplayLines();

                    updateTimer = 0;
                    break;
                case false when spawnedLines.Count > 0:
                    HideLines();
                    break;
            }

            if (Displaying)
            {
                UpdateLines();
            }
        }

        private void UpdateLines()
        {
            updateTimer += Time.deltaTime;
            if (updateTimer < updateInterval / spawnedLines.Count) return;
            
            updateTimer = 0;
            UpdateLine(spawnedLines[updateIndex]);
                    
            updateIndex = (updateIndex + 1) % spawnedLines.Count;
        }

        private void UpdateLine(LineVisualizer spawnedLine)
        {
            List<Vector3> positions = GetLine(spawnedLine.StartIndex);
            spawnedLine.DisplayLine(positions);
        }

        private void HideLines()
        {
            foreach (LineVisualizer line in spawnedLines)
            {
                line.gameObject.SetActive(false);
            }
            spawnedLines.Clear();
        }

        private void DisplayLines()
        {
            foreach (List<EnemySpawnPoint> spawnPoints in enemySpawner.SpawnPoints.Values)
            {
                foreach (EnemySpawnPoint enemySpawnPoint in spawnPoints)
                {
                    Vector3 position = enemySpawnPoint.transform.position;
                    PathIndex startIndex = PathManager.GetIndex(position.x, position.z);

                    List<Vector3> positions = GetLine(startIndex);
                    LineVisualizer spawned = linePrefab.GetAtPosAndRot<LineVisualizer>(Vector3.up * .2f, linePrefab.transform.rotation);
                    spawned.StartIndex = startIndex;
                    spawned.DisplayLine(positions);
                    spawnedLines.Add(spawned);
                }
            }
        }

        private List<Vector3> GetLine(PathIndex startIndex)
        {
            HashSet<Vector3> path = new HashSet<Vector3>();
            PathIndex index = startIndex;
            int chunkIndex = pathManager.ChunkIndexToListIndex[index.ChunkIndex];
            byte direction = pathManager.PathChunks.Value.PathChunks[chunkIndex].Directions[index.GridIndex];

            int i = 100;
            while (direction != byte.MaxValue && i-- > 0 && path.Add(PathManager.GetPos(index).xzy))
            {
                chunkIndex = pathManager.ChunkIndexToListIndex[index.ChunkIndex];
                direction = pathManager.PathChunks.Value.PathChunks[chunkIndex].Directions[index.GridIndex];
                
                float2 dir = PathManager.ByteToDirection(direction);
                index = PathManager.GetIndex(PathManager.GetPos(index).xz + dir);
            }

            return path.ToList();
        }
    }
}