using System.Collections.Generic;
using Pathfinding;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Unity.Mathematics;
using UnityEngine;

namespace Chunks
{
    public class ChunkMaskHandler : MonoBehaviour
    {
        [Title("References")]
        [SerializeField]
        private GroundGenerator groundGenerator;
        
        [SerializeField]
        private ChunkMask maskPrefab;
        
        [Title("Fade Duration")]
        [SerializeField]
        private float fadeIn = 0.5f;
        
        [SerializeField]
        private float fadeOut = 0.5f;
        
        [Title("Edge Threshold")]
        [SerializeField]
        private float distanceThreshold = 4f;
        
        private readonly Dictionary<int3, ChunkMask> masks = new Dictionary<int3, ChunkMask>();

        private void OnEnable()
        {
            groundGenerator.OnChunkGenerated += OnChunksGenerated;
        }

        private void OnDisable()
        {
            groundGenerator.OnChunkGenerated -= OnChunksGenerated;
        }

        public void CreateMask(Chunk chunk, Adjacencies defaultAdjacencies)
        {
            Vector3 position = chunk.Position + Vector3.up * 0.02f;
            ChunkMask mask = maskPrefab.GetAtPosAndRot<ChunkMask>(position, Quaternion.identity);
            mask.SetAdjacencies(defaultAdjacencies);
            mask.FadeIn(fadeIn);
            masks.Add(chunk.ChunkIndex, mask);
        }

        public void RemoveMask(Chunk chunk)
        {
            if (!masks.TryGetValue(chunk.ChunkIndex, out ChunkMask mask))
            {
                return;
            }

            mask.FadeOut(fadeOut);
            masks.Remove(chunk.ChunkIndex);
        }

        public bool IsMasked(int3 chunkIndex, Cell cell)
        {
            if (!masks.TryGetValue(chunkIndex, out ChunkMask mask))
            {
                return false;
            }

            Vector3 chunkPos = groundGenerator.ChunkWaveFunction.Chunks[chunkIndex].Position;
            Vector3 relativePosition = (cell.Position + groundGenerator.ChunkWaveFunction.GridScale / 2.0f) - chunkPos;

            if ((mask.Adjacencies & Adjacencies.North) > 0 
                && groundGenerator.ChunkScale.z - relativePosition.z < distanceThreshold)
            {
                return false;
            }
            
            if ((mask.Adjacencies & Adjacencies.South) > 0 
                && relativePosition.z < distanceThreshold)
            {
                return false;
            }
            
            if ((mask.Adjacencies & Adjacencies.East) > 0 
                && groundGenerator.ChunkScale.x - relativePosition.x < distanceThreshold)
            {
                return false;
            }
            
            if ((mask.Adjacencies & Adjacencies.West) > 0 
                && relativePosition.x < distanceThreshold)
            {
                return false;
            }

            return true;
        }

        private void OnChunksGenerated(Chunk chunk)
        {
            foreach (KeyValuePair<int3, ChunkMask> kvp in masks)
            {
                Adjacencies adjacencies = GetAdjacencies(groundGenerator.ChunkWaveFunction.Chunks[kvp.Key]);
                kvp.Value.SetAdjacencies(adjacencies);
            }
        }

        private Adjacencies GetAdjacencies(IChunk chunk)
        {
            Adjacencies adjacencies = (IsValid(chunk.AdjacentChunks[0]) ? Adjacencies.East : Adjacencies.None) |
                                      (IsValid(chunk.AdjacentChunks[1]) ? Adjacencies.West : Adjacencies.None) |
                                      (IsValid(chunk.AdjacentChunks[4]) ? Adjacencies.North : Adjacencies.None) |
                                      (IsValid(chunk.AdjacentChunks[5]) ? Adjacencies.South : Adjacencies.None);
            return adjacencies;

            bool IsValid(IChunk adjacentChunk)
            {
                return adjacentChunk != null && !masks.ContainsKey(adjacentChunk.ChunkIndex);
            }
        }
    }
}