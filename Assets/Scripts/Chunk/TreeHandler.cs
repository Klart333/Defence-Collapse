using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using WaveFunctionCollapse;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Mathematics;

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
        private Mesh[] groundTreeMeshes;
        
        private readonly Dictionary<int3, List<TreeGrower>> treeGrowersByChunk = new Dictionary<int3, List<TreeGrower>>();
        private HashSet<Mesh> groundTreeMeshSet = new HashSet<Mesh>();

        private void Awake()
        {
            groundTreeMeshSet = groundTreeMeshes.ToHashSet();
        }

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
            if (!groundTreeMeshSet.Contains(cell.PossiblePrototypes[0].MeshRot.Mesh)) return;
            
            TreeGrower spawned = treeGrowerPrefab.GetAtPosAndRot<TreeGrower>(cell.Position, Quaternion.identity);
            spawned.Cell = cell;

            Chunk chunk = groundGenerator.ChunkWaveFunction.Chunks[chunkIndex.Index];

            if (treeGrowersByChunk.TryGetValue(chunk.ChunkIndex, out List<TreeGrower> value))
            {
                value.Add(spawned);
            }
            else
            {
                treeGrowersByChunk.Add(chunk.ChunkIndex, new List<TreeGrower> { spawned });
                chunk.OnCleared += ChunkCleared;
            }
            return;

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
            GrowTrees(chunk.ChunkIndex).Forget(Debug.LogError);
        }

        private async UniTask GrowTrees(int3 chunkIndex)
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
            
                    grower.GrowTrees().Forget(Debug.LogError);
                    grower.HasGrown = true;
                    await UniTask.Yield();
                }
            }
            
            if (treeGrowersByChunk.TryGetValue(chunkIndex, out List<TreeGrower> value))
            {
                foreach (TreeGrower grower in value)
                {
                    grower.gameObject.SetActive(false);
                }
                treeGrowersByChunk.Remove(chunkIndex);
            }
        }
    }
}
