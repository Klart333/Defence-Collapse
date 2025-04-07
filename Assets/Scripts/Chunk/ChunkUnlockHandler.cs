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
            groundGenerator.OnChunkGenerated += RemoveLockedChunk;
            
            cam = Camera.main;
        }

        private void OnDisable()
        {
            groundGenerator.OnLockedChunkGenerated -= SetupLockedChunk;
            groundGenerator.OnChunkGenerated -= RemoveLockedChunk;
        }

        private void Update()
        {
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
                UnlockChunk(chunk);
                
                unlocker.OnChunkUnlocked -= OnUnlockerOnOnChunkUnlocked;
            }
        }
        
        private void RemoveLockedChunk(Chunk chunk)
        {
            if (unlockers.TryGetValue(chunk, out ChunkUnlocker unlocker))
            {
                unlocker.gameObject.SetActive(false);
                unlockers.Remove(chunk);
            }
        }

        public void UnlockChunk(Chunk chunk)
        {
            //chunk.Clear(groundGenerator.ChunkWaveFunction.GameObjectPool);
            groundGenerator.LoadChunk(chunk).Forget(Debug.LogError);
        }
    }
}