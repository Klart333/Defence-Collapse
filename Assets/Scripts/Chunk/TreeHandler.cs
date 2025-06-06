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
        private ChunkMaskHandler chunkMaskHandler;
        
        [SerializeField]
        private GroundGenerator groundGenerator;
        
        [Title("Tree")]
        [SerializeField]
        private TreeGrower treeGrowerPrefab;
        
        [SerializeField]
        private int treeMaterialIndex = 1;
        
        private readonly Dictionary<int3, List<TreeGrower>> treeGrowersByChunk = new Dictionary<int3, List<TreeGrower>>();
        
        private void OnEnable()
        {
            groundGenerator.OnChunkGenerated += OnChunkGenerated;
            groundGenerator.OnCellCollapsed += OnCellCollapsed;
        }

        private void OnDisable()
        {
            groundGenerator.OnChunkGenerated -= OnChunkGenerated;
            groundGenerator.OnCellCollapsed -= OnCellCollapsed;
        }

        private void OnCellCollapsed(ChunkIndex chunkIndex)
        {
            Cell cell = groundGenerator.ChunkWaveFunction[chunkIndex];
            if (!cell.PossiblePrototypes[0].MaterialIndexes.Contains(treeMaterialIndex)) return;
            
            TreeGrower spawned = treeGrowerPrefab.GetAtPosAndRot<TreeGrower>(cell.Position, Quaternion.identity);
            spawned.ChunkKey = chunkIndex.Index;
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
                }
                
                treeGrowersByChunk.Remove(chunk.ChunkIndex);
            }
        }

        private void OnChunkGenerated(Chunk chunk)
        {
            GrowTrees(chunk.ChunkIndex).Forget();
        }

        private async UniTaskVoid GrowTrees(int3 chunkIndex)
        {
            foreach (KeyValuePair<int3, List<TreeGrower>> kvp in treeGrowersByChunk)
            {
                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    TreeGrower grower = kvp.Value[i];
                    if (grower.HasGrown || chunkMaskHandler.IsMasked(kvp.Key, grower.Cell))
                    {
                        continue;
                    }
            
                    grower.GrowTrees().Forget();
                    grower.HasGrown = true;
                    await UniTask.Yield();
                }
            }
            
            if (treeGrowersByChunk.TryGetValue(chunkIndex, out List<TreeGrower> value))
            {
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
                    treeGrowersByChunk.Remove(chunkIndex);
                }
            }
        }

        private void GrowerOnPlaced(TreeGrower grower)
        {
            grower.OnPlaced -= GrowerOnPlaced;
            grower.gameObject.SetActive(false);
            
            int3 key = grower.ChunkKey;
            if (treeGrowersByChunk.TryGetValue(key, out List<TreeGrower> list))
            {
                list.RemoveSwapBack(grower);
                if (list.Count == 0)
                {
                    treeGrowersByChunk.Remove(key);
                }
            }
        }
    }
}
