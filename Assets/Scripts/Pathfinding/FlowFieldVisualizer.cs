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
        private InputManager inputManager;

        private bool isDisplaying;
        private float updateTimer;
        private int updateIndex;
        
        private bool Displaying => isDisplaying || BarricadePlacer.Displaying;
        
        private void OnEnable()
        {
            GetPathManager().Forget();
            GetInputManager().Forget();
        }

        private async UniTaskVoid GetPathManager()
        {
            pathManager = await PathManager.Get();
        }
        
        private async UniTaskVoid GetInputManager()
        {
            inputManager = await InputManager.Get();
        }

        private void Update()
        {
            if (inputManager.Tab.WasPerformedThisFrame())
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
                    PathIndex startIndex = PathUtility.GetIndex(position.x, position.z);

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
            HashSet<Vector3> path = new HashSet<Vector3> {PathUtility.GetPos(startIndex)};
            PathIndex index = startIndex;
            int chunkIndex = pathManager.ChunkIndexToListIndex[index.ChunkIndex];
            byte direction = pathManager.PathChunks.Value.PathChunks[chunkIndex].Directions[index.GridIndex];
            bool pointsAreUnique = true;
            int i = 256;
            while (direction != byte.MaxValue && i-- > 0 && pointsAreUnique)
            {
                if (!pathManager.ChunkIndexToListIndex.TryGetValue(index.ChunkIndex, out chunkIndex) 
                    || index.GridIndex < 0 || index.GridIndex >= PathUtility.GRID_LENGTH) return path.ToList();
                
                byte lastDirection = direction;
                direction = pathManager.PathChunks.Value.PathChunks[chunkIndex].Directions[index.GridIndex];
                if (direction != lastDirection)
                {
                    pointsAreUnique = path.Add(PathUtility.GetPos(index));
                }
                
                float2 dir = PathUtility.ByteToDirection(direction);
                index = PathUtility.GetIndex(PathUtility.GetPos(index).xz + dir * pathManager.CellScale);
            }

            return path.ToList();
        }
    }
}