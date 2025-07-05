using System.Collections.Generic;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Unity.Mathematics;
using UnityEngine;
using System.Linq;

namespace Chunks
{
    public class ShellGrassHandler : MonoBehaviour
    {
        [Title("References")]
        [SerializeField]
        private ChunkMaskHandler chunkMaskHandler;
        
        [SerializeField]
        private GroundGenerator groundGenerator;
        
        [SerializeField]
        private ProtoypeMeshes protoypeMeshes;
        
        [Title("Grass")]
        [SerializeField]
        private ShellGrassGrower grassGrowerPrefab;
        
        [SerializeField]
        private int grassMaterialIndex = 0;
        
        private readonly Dictionary<int3, List<ShellGrassGrower>> grassGrowersByChunk = new Dictionary<int3, List<ShellGrassGrower>>();
        
        private void OnEnable()
        {
            groundGenerator.OnCellCollapsed += OnCellCollapsed;
        }

        private void OnDisable()
        {
            groundGenerator.OnCellCollapsed -= OnCellCollapsed;
        }

        private void OnCellCollapsed(ChunkIndex chunkIndex)
        {
            Cell cell = groundGenerator.ChunkWaveFunction[chunkIndex];
            if (!cell.PossiblePrototypes[0].MaterialIndexes.Contains(grassMaterialIndex)) return;
            
            Quaternion rotation = Quaternion.Euler(0, 90 * cell.PossiblePrototypes[0].MeshRot.Rot, 0);
            ShellGrassGrower spawned = grassGrowerPrefab.GetAtPosAndRot<ShellGrassGrower>(cell.Position, rotation);
            spawned.ChunkKey = chunkIndex.Index;
            spawned.Cell = cell;

            Mesh mesh = protoypeMeshes[cell.PossiblePrototypes[0].MeshRot.MeshIndex];
            int submeshIndex = 0;
            for (int i = 0; i < cell.PossiblePrototypes[0].MaterialIndexes.Length; i++)
            {
                if (cell.PossiblePrototypes[0].MaterialIndexes[i] == grassMaterialIndex)
                {
                    submeshIndex = i;
                }
            }
            spawned.DisplayGrass(mesh, submeshIndex);

            Chunk chunk = groundGenerator.ChunkWaveFunction.Chunks[chunkIndex.Index];

            if (grassGrowersByChunk.TryGetValue(chunkIndex.Index, out List<ShellGrassGrower> value))
            {
                value.Add(spawned);
            }
            else
            {
                grassGrowersByChunk.Add(chunkIndex.Index, new List<ShellGrassGrower> { spawned });
                chunk.OnCleared += ChunkCleared;
            }

            void ChunkCleared()
            {
                chunk.OnCleared -= ChunkCleared;

                if (!grassGrowersByChunk.TryGetValue(chunkIndex.Index, out List<ShellGrassGrower> growers)) return;
                
                for (int i = 0; i < growers.Count; i++)
                {
                    growers[i].gameObject.SetActive(false);
                }
                
                grassGrowersByChunk.Remove(chunk.ChunkIndex);
            }
        }
    }
}