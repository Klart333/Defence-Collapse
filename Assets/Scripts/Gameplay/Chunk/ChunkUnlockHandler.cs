using System.Collections.Generic;
using Gameplay;
using Gameplay.Event;
using InputCamera;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;
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

        [SerializeField]
        private Canvas canvas;

        [Title("Cost")]
        [SerializeField]
        private float startingChunkCost = 200;

        [SerializeField]
        private float chunkCostIncrease = 3;

        private Camera cam;
        
        private readonly Dictionary<Chunk, ChunkUnlocker> unlockers = new Dictionary<Chunk, ChunkUnlocker>();
        
        private float Cost { get; set; }
        
        private void OnEnable()
        {
            Events.OnGroundChunkGenerated += SetupLockedChunk; 
            
            cam = Camera.main;
            Cost = startingChunkCost;
        }

        private void OnDisable()
        {
            Events.OnGroundChunkGenerated -= SetupLockedChunk;
        }

        private void Update()
        {
            if (groundGenerator.IsGenerating || InputManager.MouseOverUI() || GameManager.Instance.IsGameOver)
            {
                foreach (ChunkUnlocker unlocker in unlockers.Values)
                {
                    if (!unlocker.Hovered)
                    {
                        unlocker.SetShowing(false);
                    }
                }

                return;
            }
            
            Vector3 point = Utility.Math.GetGroundIntersectionPoint(cam, Mouse.current.position.ReadValue());
            foreach (KeyValuePair<Chunk, ChunkUnlocker> kvp in unlockers)
            {
                kvp.Value.SetShowing(kvp.Key.ContainsPoint(point, Vector3.one * 2));
            }
        }

        private void SetupLockedChunk(Chunk chunk)
        {
            if (!IsChunkAdjacent(chunk))
            {
                return;
            }
            
            Vector3 pos = chunk.Position + ((Vector3)chunk.ChunkSize).XyZ(0) / 2f + groundGenerator.ChunkWaveFunction.CellSize.XyZ(0) / 2.0f + Vector3.up * 2f;
            ChunkUnlocker unlocker = unlockPrefab.Get<ChunkUnlocker>();
            unlocker.transform.SetParent(canvas.transform, false);
            unlocker.Cost = Cost;
            unlocker.Canvas = canvas;
            unlocker.TargetPosition = pos;
            unlocker.DisplayCost();
            
            unlockers.Add(chunk, unlocker);
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

        private bool IsChunkAdjacent(Chunk chunk)
        {
            return true;
        }

        public void UnlockChunk(Chunk chunk)
        {
            groundGenerator.LoadChunk(chunk.ChunkIndex);
            Cost *= chunkCostIncrease;

            foreach (ChunkUnlocker unlocker in unlockers.Values)
            {
                unlocker.Cost = Cost;
                unlocker.DisplayCost();
            }

            PersistantGameStats.CurrentPersistantGameStats.ChunksExplored++;
        }
    }
}