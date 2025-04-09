using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using WaveFunctionCollapse;
using UnityEngine;
using System.Linq;
using Sirenix.OdinInspector;

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
        
        private readonly Dictionary<Chunk, List<TreeGrower>> treeGrowersByChunk = new Dictionary<Chunk, List<TreeGrower>>();

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
            if (!groundTreeMeshes.Contains(cell.PossiblePrototypes[0].MeshRot.Mesh)) return;
            
            TreeGrower spawned = treeGrowerPrefab.GetAtPosAndRot<TreeGrower>(cell.Position, Quaternion.identity);
            spawned.Cell = cell;

            Chunk chunk = groundGenerator.ChunkWaveFunction.Chunks[chunkIndex.Index];

            if (treeGrowersByChunk.TryGetValue(chunk, out List<TreeGrower> value))
            {
                value.Add(spawned);
            }
            else
            {
                treeGrowersByChunk.Add(chunk, new List<TreeGrower>{ spawned });
                chunk.OnCleared += ChunkCleared;
            }
            return;

            void ChunkCleared()
            {
                chunk.OnCleared -= ChunkCleared;

                List<TreeGrower> growers = treeGrowersByChunk[chunk];
                for (int i = 0; i < growers.Count; i++)
                {
                    growers[i].Clear();
                    growers[i].gameObject.SetActive(false);
                }
                
                treeGrowersByChunk.Remove(chunk);
            }
        }


        private async void OnChunkGenerated(Chunk chunk)
        {
            foreach (KeyValuePair<Chunk,List<TreeGrower>> kvp in treeGrowersByChunk)
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
            
        }
    }
}
