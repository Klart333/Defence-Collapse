using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
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
        
        private readonly Dictionary<IChunk, ChunkMask> masks = new Dictionary<IChunk, ChunkMask>();

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
            masks.Add(chunk, mask);
        }

        public void RemoveMask(Chunk chunk)
        {
            if (!masks.TryGetValue(chunk, out ChunkMask mask))
            {
                return;
            }

            mask.FadeOut(fadeOut);
            masks.Remove(chunk);
        }

        public bool IsMasked(Chunk chunk, Cell cell)
        {
            if (!masks.TryGetValue(chunk, out ChunkMask mask))
            {
                return false;
            }

            return true;
        }

        private void OnChunksGenerated(Chunk chunk)
        {
            foreach (KeyValuePair<IChunk, ChunkMask> kvp in masks)
            {
                Adjacencies adjacencies = GetAdjacencies(kvp.Key);
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
                return adjacentChunk != null && !masks.ContainsKey(adjacentChunk);
            }
        }
    }
}