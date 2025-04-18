using System.Collections.Generic;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using WaveFunctionCollapse;
using UnityEngine;

namespace Chunks
{
    public class ChunkUnlockHandler : MonoBehaviour
    {
        [SerializeField]
        private ChunkUnlocker unlockPrefab;

        [SerializeField]
        private GroundGenerator groundGenerator;

        private Camera cam;
        
        private readonly Dictionary<Chunk, ChunkUnlocker> unlockers = new Dictionary<Chunk, ChunkUnlocker>();
        
        private void OnEnable()
        {
            groundGenerator.OnLockedChunkGenerated += SetupLockedChunk; 
            
            cam = Camera.main;
        }

        private void OnDisable()
        {
            groundGenerator.OnLockedChunkGenerated -= SetupLockedChunk;
        }

        private void Update()
        {
            if (groundGenerator.IsGenerating)
            {
                foreach (ChunkUnlocker unlocker in unlockers.Values)
                {
                    unlocker.SetShowing(false);
                }

                return;
            }
            
            Vector3 point = Math.GetGroundIntersectionPoint(cam, Mouse.current.position.ReadValue());
            foreach (KeyValuePair<Chunk, ChunkUnlocker> kvp in unlockers)
            {
                kvp.Value.SetShowing(kvp.Key.ContainsPoint(point, Vector3.one * 2));
            }
        }

        private void SetupLockedChunk(Chunk chunk)
        {
            Vector3 pos = chunk.Position + chunk.ChunkSize + Vector3.up * 2f;
            ChunkUnlocker unlocker = unlockPrefab.GetAtPosAndRot<ChunkUnlocker>(pos, unlockPrefab.transform.rotation);
            unlockers.Add(chunk, unlocker);
            unlocker.Cost = 50;
            unlocker.DisplayCost();
            
            unlocker.OnChunkUnlocked += OnUnlockerOnOnChunkUnlocked;
            return;

            void OnUnlockerOnOnChunkUnlocked()
            {
                unlocker.gameObject.SetActive(false);
                unlockers.Remove(chunk);
                UnlockChunk(chunk);
                
                unlocker.OnChunkUnlocked -= OnUnlockerOnOnChunkUnlocked;
            }
        }

        public void UnlockChunk(Chunk chunk)
        {
            groundGenerator.ChunkWaveFunction.RemoveChunk(chunk.ChunkIndex, out _);
            Chunk newChunk = groundGenerator.ChunkWaveFunction.LoadChunk(chunk.ChunkIndex, chunk.ChunkSize, groundGenerator.DefaultPrototypeInfoData, false);
            
            groundGenerator.LoadChunk(newChunk).Forget();
        }
    }
}