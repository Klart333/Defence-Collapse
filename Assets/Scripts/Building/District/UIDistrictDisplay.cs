using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Buildings.District
{
    public class UIDistrictDisplay : SerializedMonoBehaviour
    {
        [Title("Display")]
        [SerializeField]
        private GameObject confirmButton;
        
        [SerializeField]
        private DistrictPlacer displayPrefab;

        [SerializeField]
        private float maxDelay = 2f;

        [SerializeField]
        private float maxDelayDistance = 20;
        
        [Title("District")]
        [SerializeField]
        private DistrictHandler districtHandler;
        
        [SerializeField]
        private IChunkWaveFunction districtGenerator;
        
        private Queue<DistrictPlacer> selectedPlacers = new Queue<DistrictPlacer>();
        private List<DistrictPlacer> spawnedPlacers = new List<DistrictPlacer>();
        
        private DistrictType currentType;
        
        private async void OnEnable()
        {
            Events.OnDistrictClicked += OnDistrictClicked;

            await UniTask.WaitUntil(() => InputManager.Instance != null);
            InputManager.Instance.Cancel.performed += CancelPerformed; 
        }

        private void OnDisable()
        {
            Events.OnDistrictClicked -= OnDistrictClicked;
            InputManager.Instance.Cancel.performed -= CancelPerformed; 
        }
        
        private void CancelPerformed(InputAction.CallbackContext obj)
        {
            HideAnimated();
        }

        private void OnDistrictClicked(DistrictType districtType)
        {
            currentType = districtType;
            
            Hide();
            
            Display(districtGenerator.ChunkWaveFunction);
        }
        
        private void Display(ChunkWaveFunction chunkWaveFunction)
        {
            Vector3 scale = districtGenerator.ChunkSize * 0.75f;

            Bounds bounds = GetBounds(chunkWaveFunction);

            float maxDistance = Vector2.Distance(bounds.Min, bounds.Max);
            float scaledDelay = maxDelay * Mathf.Clamp01(maxDistance / maxDelayDistance);
            foreach (Chunk chunk in chunkWaveFunction.Chunks)
            {
                if (districtHandler.IsBuilt(chunk)) continue;
                    
                Vector3 pos = chunk.Position + Vector3.up;
                DistrictPlacer spawned = displayPrefab.GetAtPosAndRot<DistrictPlacer>(pos, quaternion.identity);
                spawned.Index = DistrictHandler.GetDistrictIndex(pos, districtGenerator);
                    
                spawned.transform.localScale = Vector3.zero;
                float delay = scaledDelay * (Vector2.Distance(bounds.Min, chunk.Position.XZ()) / maxDistance);
                spawned.transform.DOScale(scale, 0.5f).SetEase(Ease.OutBounce).SetDelay(delay);
                spawnedPlacers.Add(spawned);
                    
                spawned.OnSelected += PlacerOnOnSelected;
            }
        }
        
        private void PlacerOnOnSelected(DistrictPlacer selectedPlacer)
        {
            selectedPlacers.Enqueue(selectedPlacer);
            selectedPlacer.SetSelected();

            switch (selectedPlacers.Count)
            {
                case 1:
                    return;
                case > 2:
                {
                    var removedPlacer = selectedPlacers.Dequeue();
                    removedPlacer.Unselect();
                    break;
                }
            }

            Bounds bounds = GetBounds(selectedPlacers);
            int width = (int)bounds.Max.x - (int)bounds.Min.x + 1;
            int depth = (int)bounds.Max.y - (int)bounds.Min.y + 1;

            int includedPlacers = 0;
            for (int i = 0; i < spawnedPlacers.Count; i++)
            {
                int2 index = spawnedPlacers[i].Index;
                if (bounds.Contains(new Vector2(index.x, index.y)))
                {
                    includedPlacers++;
                }
            }

            bool isSquare = includedPlacers >= width * depth;
            bool canBuild = isSquare && DistrictHandler.CanBuildDistrict(width, depth, currentType);
            
            confirmButton.SetActive(canBuild);
            Color color = canBuild ? Color.green : Color.red;

            for (int i = 0; i < spawnedPlacers.Count; i++)
            {
                int2 index = spawnedPlacers[i].Index;
                if (bounds.Contains(new Vector2(index.x, index.y)))
                {
                    spawnedPlacers[i].SetSelected(color);
                }
                else
                {
                    spawnedPlacers[i].Unselect();
                }
            }
        }

        public void PlacementConfirmed()
        {
            Bounds bounds = GetPositionBounds(selectedPlacers);
            
            List<Chunk> chunks = new List<Chunk>();
            foreach (Chunk chunk in districtGenerator.ChunkWaveFunction.Chunks)
            {
                if (bounds.Contains(chunk.Position.XZ()))
                {
                    chunks.Add(chunk);
                }
            }
            
            HideAnimated();
            districtHandler.BuildDistrict(chunks, currentType);
        }
        
        private static Bounds GetBounds(ChunkWaveFunction chunkWaveFunction)
        {
            Vector2 min = Vector2.positiveInfinity;
            Vector2 max = Vector2.negativeInfinity;
            
            foreach (Chunk chunk in chunkWaveFunction.Chunks) 
            {
                Vector3 pos = chunk.Position;
                if (pos.x < min.x) min.x = pos.x;
                if (pos.z < min.y) min.y = pos.z;
                if (pos.x > max.x) max.x = pos.x;
                if (pos.z > max.y) max.y = pos.z;
            }

            return new Bounds
            {
                Min = min,
                Max = max
            };
        }
        
        private static Bounds GetBounds(IEnumerable<DistrictPlacer> placers)
        {
            int minX = int.MaxValue, minZ = int.MaxValue, maxX = 0, maxZ = 0;
            foreach (DistrictPlacer placer in placers)
            {
                if (placer.Index.x < minX) minX = placer.Index.x;
                if (placer.Index.x > maxX) maxX = placer.Index.x;
                if (placer.Index.y < minZ) minZ = placer.Index.y;
                if (placer.Index.y > maxZ) maxZ = placer.Index.y;
            }

            return new Bounds
            {
                Min = new Vector2(minX, minZ),
                Max = new Vector2(maxX, maxZ),
            };
        }
        
        private static Bounds GetPositionBounds(IEnumerable<DistrictPlacer> placers)
        {
            Vector2 min = Vector2.positiveInfinity;
            Vector2 max = Vector2.negativeInfinity;

            foreach (DistrictPlacer placer in placers)
            {
                Vector3 pos = placer.transform.position;
                if (pos.x < min.x) min.x = pos.x;
                if (pos.z < min.y) min.y = pos.z;
                if (pos.x > max.x) max.x = pos.x;
                if (pos.z > max.y) max.y = pos.z;
            }

            return new Bounds
            {
                Min = min,
                Max = max,
            };
        }
        
        public void Hide()
        {
            for (int i = 0; i < spawnedPlacers.Count; i++)
            {
                spawnedPlacers[i].gameObject.SetActive(false);
                spawnedPlacers[i].OnSelected -= PlacerOnOnSelected;
            }
            
            Clear();
        }

        public void HideAnimated()
        {
            if (spawnedPlacers.Count == 0)
            {
                Clear();
                return;
            }
            
            Vector3 min = spawnedPlacers[0].transform.position;
            float maxDistance = Vector3.Distance(min, spawnedPlacers[^1].transform.position);
            float scaledDelay = maxDelay * Mathf.Clamp01(maxDistance / maxDelayDistance);
            for (int i = 0; i < spawnedPlacers.Count; i++)
            {
                float delay = scaledDelay * (Vector3.Distance(min, spawnedPlacers[i].transform.position) / maxDistance);
                Transform placer = spawnedPlacers[i].transform;
                placer.DOScale(0, 0.5f).SetEase(Ease.InCubic).SetDelay(delay).OnComplete(() =>
                {
                    placer.gameObject.SetActive(false);
                });
                
                spawnedPlacers[i].OnSelected -= PlacerOnOnSelected;
            }
            
            Clear();
        }

        private void Clear()
        {
            spawnedPlacers.Clear();
            selectedPlacers.Clear();

            confirmButton.SetActive(false);
        }
        
        private struct Bounds
        {
            public Vector2 Min, Max;

            public bool Contains(Vector2 vec) => 
                   vec.x >= Min.x && vec.x <= Max.x 
                && vec.y >= Min.y && vec.y <= Max.y;
        }
    }
}
