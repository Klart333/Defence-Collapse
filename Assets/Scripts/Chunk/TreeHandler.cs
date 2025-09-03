using Random = UnityEngine.Random;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Unity.Mathematics;
using Unity.Collections;
using System.Linq;
using UnityEngine;

namespace Chunks
{
    public class TreeHandler : MonoBehaviour
    {
        [Title("References")]
        [SerializeField]
        private GroundGenerator groundGenerator;
        
        [Title("Tree")]
        [SerializeField]
        private TreeGrower treeGrowerPrefab;
        
        [SerializeField]
        private int treeMaterialIndex = 1;

        [Title("Tree Settings")]
        [SerializeField]
        private int groupCount = 1;
        
        [SerializeField, Range(0.0f, 1.0f)]
        private float groupingFactor = 0.4f;

        private Dictionary<int3, List<TreeGrower>> treeGrowersByChunk = new Dictionary<int3, List<TreeGrower>>();
        
        private Dictionary<int2, int> builtIndexesMap = new Dictionary<int2, int>();

        private int2[] neighbours = new int2[4]
        {
            new int2(1, 0),
            new int2(-1, 0),
            new int2(0, 1),
            new int2(0, -1),
        };

        private void OnEnable()
        {
            groundGenerator.OnGenerationFinished += OnGenerationFinished;
            groundGenerator.OnCellCollapsed += OnCellCollapsed;
        }

        private void OnDisable()
        {
            groundGenerator.OnGenerationFinished -= OnGenerationFinished;
            groundGenerator.OnCellCollapsed -= OnCellCollapsed;
        }

        private void OnCellCollapsed(ChunkIndex chunkIndex)
        {
            Cell cell = groundGenerator.ChunkWaveFunction[chunkIndex];
            if (!cell.PossiblePrototypes[0].MaterialIndexes.Contains(treeMaterialIndex)) return;
            
            TreeGrower spawned = treeGrowerPrefab.GetAtPosAndRot<TreeGrower>(cell.Position, Quaternion.identity);
            spawned.ChunkIndex = chunkIndex;
            spawned.Cell = cell;

            Chunk chunk = groundGenerator.ChunkWaveFunction.Chunks[chunkIndex.Index];

            if (treeGrowersByChunk.TryGetValue(chunkIndex.Index, out List<TreeGrower> value))
            {
                value.Add(spawned);
            }
            else
            {
                treeGrowersByChunk.Add(chunkIndex.Index, new List<TreeGrower> { spawned });
                chunk.OnCleared += ChunkCleared;
            }

            void ChunkCleared()
            {
                chunk.OnCleared -= ChunkCleared;

                if (!treeGrowersByChunk.TryGetValue(chunkIndex.Index, out List<TreeGrower> growers)) return;
                
                for (int i = 0; i < growers.Count; i++)
                {
                    growers[i].ClearTrees();
                    growers[i].gameObject.SetActive(false);
                    int2 totalChunkIndex = growers[i].ChunkIndex.Index.xz.MultiplyByAxis(groundGenerator.ChunkSize.xz) + growers[i].ChunkIndex.CellIndex.xz;
                    builtIndexesMap.Remove(totalChunkIndex);
                }
                
                treeGrowersByChunk.Remove(chunk.ChunkIndex);
            }
        }

        private void OnGenerationFinished()
        {
            GrowTrees().Forget();
        }

        private async UniTaskVoid GrowTrees()
        {
            List<int3> chunksGrown = new List<int3>();
            foreach (KeyValuePair<int3, List<TreeGrower>> kvp in treeGrowersByChunk)
            {
                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    TreeGrower grower = kvp.Value[i];
                    if (grower.HasGrown)
                    {
                        continue;
                    }
                    chunksGrown.Add(kvp.Key);
                    int groupIndex = 0;
                    if (groupCount > 1)
                    {
                        int2 totalChunkIndex = grower.ChunkIndex.Index.xz.MultiplyByAxis(groundGenerator.ChunkSize.xz) + grower.ChunkIndex.CellIndex.xz;
                        groupIndex = GetGroupIndex(totalChunkIndex);
                        builtIndexesMap.Add(totalChunkIndex, groupIndex);
                    }
                    
                    grower.GrowTrees(groupIndex).Forget();
                    grower.HasGrown = true;
                    await UniTask.Yield();
                }
                
            }

            //DeallocateGrownChunks(chunksGrown);
        }

        private void DeallocateGrownChunks(List<int3> chunksGrown)
        {
            foreach (int3 chunks in chunksGrown)
            {
                if (!treeGrowersByChunk.TryGetValue(chunks, out List<TreeGrower> value)) continue;
                
                bool removeChunk = true;
                foreach (TreeGrower grower in value)
                {
                    if (grower.ShouldRemoveWhenPlaced)
                    {
                        removeChunk = false;
                        grower.OnPlaced += GrowerOnPlaced;

                        continue;
                    }
                    
                    grower.gameObject.SetActive(false);
                }

                if (removeChunk)
                {
                    treeGrowersByChunk.Remove(chunks);
                }
            }
        }

        private int GetGroupIndex(int2 index)
        {
            for (int i = 0; i < neighbours.Length; i++)
            {
                int2 neighbour = index + neighbours[i];
                if (!builtIndexesMap.TryGetValue(neighbour, out int value)) continue;
                
                float randValue = Random.value;
                if (randValue < groupingFactor)
                {
                    return value;
                }
            }
            
            return Random.Range(0, groupCount);
        }

        private void GrowerOnPlaced(TreeGrower grower)
        {
            grower.OnPlaced -= GrowerOnPlaced;
            grower.gameObject.SetActive(false);
            
            int3 key = grower.ChunkIndex.Index;
            if (!treeGrowersByChunk.TryGetValue(key, out List<TreeGrower> list)) return;
            list.RemoveSwapBack(grower);
            if (list.Count == 0)
            {
                treeGrowersByChunk.Remove(key);
            }
        }
    }
}
