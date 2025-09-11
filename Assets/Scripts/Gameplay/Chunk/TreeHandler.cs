using System.Collections;
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

        private Coroutine growingTrees;

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
            int2 totalIndex = chunkIndex.Index.xz.MultiplyByAxis(groundGenerator.ChunkSize.xz) + chunkIndex.CellIndex.xz;
            if (builtIndexesMap.ContainsKey(totalIndex))
            {
                return;
            }
            
            Cell cell = groundGenerator.ChunkWaveFunction[chunkIndex];
            if (!cell.PossiblePrototypes[0].MaterialIndexes.Contains(treeMaterialIndex)) return;

            if (growingTrees != null)
            {
                StopCoroutine(growingTrees);
            }
            
            TreeGrower spawned = treeGrowerPrefab.GetAtPosAndRot<TreeGrower>(cell.Position, Quaternion.identity);
            spawned.ChunkIndex = chunkIndex;
            spawned.Cell = cell;

            if (treeGrowersByChunk.TryGetValue(chunkIndex.Index, out List<TreeGrower> value)) value.Add(spawned);
            else treeGrowersByChunk.Add(chunkIndex.Index, new List<TreeGrower> { spawned });
            
        }

        private void OnGenerationFinished()
        {
            growingTrees = StartCoroutine(GrowTrees());
        }

        private IEnumerator GrowTrees()
        {
            foreach (KeyValuePair<int3, List<TreeGrower>> kvp in treeGrowersByChunk)
            {
                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    TreeGrower grower = kvp.Value[i];
                    if (grower.HasGrown)
                    {
                        continue;
                    }
                    int groupIndex = 0;
                    if (groupCount > 1)
                    {
                        int2 totalChunkIndex = grower.ChunkIndex.Index.xz.MultiplyByAxis(groundGenerator.ChunkSize.xz) + grower.ChunkIndex.CellIndex.xz;
                        groupIndex = GetGroupIndex(totalChunkIndex);
                        builtIndexesMap.Add(totalChunkIndex, groupIndex);
                    }
                    
                    grower.GrowTrees(groupIndex).Forget();
                    grower.HasGrown = true;
                    yield return null;
                }
                
            }

            growingTrees = null;
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
    }
}
