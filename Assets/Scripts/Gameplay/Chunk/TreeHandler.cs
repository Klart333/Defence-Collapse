using Random = Unity.Mathematics.Random;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using Gameplay;

namespace Chunks
{
    public class TreeHandler : MonoBehaviour
    {
        [Title("References")]
        [SerializeField]
        private GroundGenerator groundGenerator;
        
        [SerializeField]
        private BuildableCornerData groundCornerData;
        
        [SerializeField]
        private ProtoypeMeshes protoypeMeshes;
        
        [Title("Tree")]
        [SerializeField]
        private TreeGrower treeGrowerPrefab;
        
        [SerializeField]
        private GroundType objectGroundType;

        [Title("Tree Settings")]
        [SerializeField]
        private int groupCount = 1;
        
        [SerializeField, Range(0.0f, 1.0f)]
        private float groupingFactor = 0.4f;

        private Dictionary<int3, List<TreeGrower>> treeGrowersByChunk = new Dictionary<int3, List<TreeGrower>>();
        
        private Dictionary<int2, int> builtIndexesMap = new Dictionary<int2, int>();

        private GameManager gameManager;
        private Coroutine growingTrees;
        private Random random;

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

            GetGameManager().Forget();
        }

        private void OnDisable()
        {
            groundGenerator.OnGenerationFinished -= OnGenerationFinished;
            groundGenerator.OnCellCollapsed -= OnCellCollapsed;
        }

        private async UniTaskVoid GetGameManager()
        {
            gameManager = await GameManager.Get();
            random = Random.CreateFromIndex(gameManager.Seed);
        }
        
        private void OnCellCollapsed(ChunkIndex chunkIndex) 
        {
            int2 totalIndex = chunkIndex.Index.xz.MultiplyByAxis(groundGenerator.ChunkSize.xz) + chunkIndex.CellIndex.xz;
            if (builtIndexesMap.ContainsKey(totalIndex))
            {
                return;
            }
            
            Cell cell = groundGenerator.ChunkWaveFunction[chunkIndex];
            Mesh mesh = protoypeMeshes.Meshes[cell.PossiblePrototypes[0].MeshRot.MeshIndex];
            BuildableCorners corners = groundCornerData.BuildableDictionary[mesh];
            if (AllCornersInvalid(corners)) return;

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
        
        private bool AllCornersInvalid(BuildableCorners corners)
        {
            for (int i = 0; i < CornerUtility.AllCorners.Length; i++)
            {
                if (corners.CornerDictionary[CornerUtility.AllCorners[i]].GroundType.HasFlag(objectGroundType))
                {
                    return false;
                }
            }       
            
            return true;
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
                    int2 totalChunkIndex = grower.ChunkIndex.Index.xz.MultiplyByAxis(groundGenerator.ChunkSize.xz) + grower.ChunkIndex.CellIndex.xz;
                    if (groupCount > 1)
                    {
                        groupIndex = GetGroupIndex(totalChunkIndex);
                        builtIndexesMap.Add(totalChunkIndex, groupIndex);
                    }

                    uint seed = gameManager.Seed + (uint)(totalChunkIndex.x * 100 + totalChunkIndex.y * 10 + i);  
                    grower.GrowTrees(groupIndex, Random.CreateFromIndex(seed)).Forget();
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
                
                float randValue = random.NextFloat();
                if (randValue < groupingFactor)
                {
                    return value;
                }
            }
            
            return random.NextInt(0, groupCount);
        }
    }
}
