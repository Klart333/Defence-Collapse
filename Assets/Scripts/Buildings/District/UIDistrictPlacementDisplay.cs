using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Gameplay.Money;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using WaveFunctionCollapse;

namespace Buildings.District
{/*
    public class UIDistrictPlacementDisplay : SerializedMonoBehaviour
    {
        public static bool Displaying = false; 
        
        [Title("Display")]
        [SerializeField]
        private GameObject confirmButton;

        [SerializeField]
        private TextMeshProUGUI costText;
        
        [SerializeField]
        private DistrictPlaceSquare displayPrefab;

        [SerializeField]
        private float maxDelay = 2f;

        [SerializeField]
        private float maxDelayDistance = 20;
        
        [Title("District")]
        [SerializeField]
        private DistrictHandler districtHandler;
        
        [SerializeField]
        private DistrictGenerator districtGenerator;

        [Title("GroundType")]
        [SerializeField]
        private Material crystalMaterial;
        
        private Queue<DistrictPlaceSquare> selectedPlacers = new Queue<DistrictPlaceSquare>();
        private List<DistrictPlaceSquare> spawnedPlacers = new List<DistrictPlaceSquare>();
        private readonly Dictionary<int3, GroundType> chunkGroupTypes = new Dictionary<int3, GroundType>();
        
        private DistrictType currentType;
        private int minRadius;
        
        private async void OnEnable()
        {
            Events.OnDistrictClicked += OnDistrictClicked;
            UIEvents.OnFocusChanged += HideAnimated;
            Events.OnGameReset += OnGameReset;

            await UniTask.WaitUntil(() => InputManager.Instance != null);
            InputManager.Instance.Cancel.performed += CancelPerformed; 
        }

        private void OnDisable()
        {
            InputManager.Instance.Cancel.performed -= CancelPerformed; 
            Events.OnDistrictClicked -= OnDistrictClicked;
            UIEvents.OnFocusChanged -= HideAnimated;
            Events.OnGameReset -= OnGameReset;
        }

        private void OnGameReset()
        {
            Displaying = false;
        }
        
        private void CancelPerformed(InputAction.CallbackContext obj)
        {
            HideAnimated();
        }

        private void OnDistrictClicked(DistrictType districtType, int radius)
        {
            UIEvents.OnFocusChanged?.Invoke();

            Displaying = true;
            currentType = districtType;
            minRadius = radius;
            
            Display(districtGenerator.ChunkWaveFunction);
        }
        
        private void Display(ChunkWaveFunction<QueryChunk> chunkWaveFunction)
        {
            Vector3 scale = districtGenerator.ChunkScale * 0.75f;
            Bounds bounds = GetBounds(chunkWaveFunction.Chunks.Values);

            float maxDistance = Vector2.Distance(bounds.Min, bounds.Max);
            float scaledDelay = maxDelay * Mathf.Clamp01(maxDistance / maxDelayDistance);
            foreach (QueryChunk chunk in chunkWaveFunction.Chunks.Values)
            {
                if (chunk.ChunkIndex.y != 0) continue;
                if (districtHandler.IsBuilt(chunk.ChunkIndex.xz)) continue;
                    
                int3 index = ChunkWaveUtility.GetDistrictIndex3(chunk.Position, districtGenerator.ChunkScale);
                if (!CheckDistrictRestriction(currentType, index)) continue; 
                
                Vector3 pos = chunk.Position + Vector3.up;
                DistrictPlaceSquare spawned = displayPrefab.GetAtPosAndRot<DistrictPlaceSquare>(pos, quaternion.identity);
                spawned.Index = index;
                
                spawned.transform.localScale = Vector3.zero;
                float delay = scaledDelay * (Vector2.Distance(bounds.Min, chunk.Position.XZ()) / maxDistance);
                spawned.transform.DOScale(scale, 0.5f).SetEase(Ease.OutBounce).SetDelay(delay);
                spawnedPlacers.Add(spawned);
                    
                spawned.OnSelected += PlacerOnOnSelected;
            }
        }
        
        private void PlacerOnOnSelected(DistrictPlaceSquare selectedPlacer)
        {
            selectedPlacers.Enqueue(selectedPlacer);
            selectedPlacer.SetSelected();

            if (selectedPlacers.Count > 2)
            {
                var removedPlacer = selectedPlacers.Dequeue();
                removedPlacer.Unselect();
            }

            spawnedPlacers.Where(x => x.Selected && !selectedPlacers.Contains(x)).ForEach(x => x.Unselect());
            
            Bounds bounds = GetBounds(spawnedPlacers.Where(x => x.Selected));
            int width = (int)bounds.Max.x - (int)bounds.Min.x + 1;
            int depth = (int)bounds.Max.y - (int)bounds.Min.y + 1;

            HashSet<int3> includedChunks = new HashSet<int3>();
            int chunkCount = 0; // Only top chunks
            for (int i = 0; i < spawnedPlacers.Count; i++)
            {
                int3 index = spawnedPlacers[i].Index;
                if (bounds.Contains(new Vector2(index.x, index.z)))
                {
                    includedChunks.Add(index);
                    chunkCount++;
                }
            }

            bool sameHeight = selectedPlacer.Index.y == selectedPlacers.Peek().Index.y;
            bool canBuild = sameHeight
                            && IsConnected(includedChunks)
                            && CanBuildDistrict(width, depth, currentType)
                            && CheckSizeRestriction(includedChunks.Count, currentType);
            
            costText.gameObject.SetActive(canBuild);
            if (canBuild)
            {
                bool canAfford = MoneyManager.Instance.CanPurchase(currentType, chunkCount, out float cost);
                canBuild = canAfford;
                costText.text = $"Cost: {cost:N0}g";
            }

            confirmButton.SetActive(canBuild);
            
            Color color = canBuild ? Color.green : Color.red;
            costText.color = color;
            for (int i = 0; i < spawnedPlacers.Count; i++)
            {
                int3 index = spawnedPlacers[i].Index;
                if (bounds.Contains(new Vector2(index.x, index.z)))
                {
                    spawnedPlacers[i].SetSelected(color);
                }
                else
                {
                    spawnedPlacers[i].Unselect();
                }
            }
        }

        private bool CheckSizeRestriction(int chunkCount, DistrictType districtType)
        {
            return districtType switch
            {
                DistrictType.TownHall => chunkCount >= minRadius * minRadius,
                _ => true,
            };
        }

        public bool CanBuildDistrict(int width, int depth, DistrictType currentType)
        {
            return currentType switch
            {
                DistrictType.Bomb => width >= 3 && depth >= 3,
                DistrictType.Church => width >= 2 && depth >= 2,
                DistrictType.TownHall => width - minRadius == 0 && depth - minRadius == 0,
                _ => true
            };
        }

        private bool IsConnected(HashSet<int3> chunks)
        {
            if (chunks.Count == 1)
            {
                return true;
            }
            
            HashSet<int3> neighbours = new HashSet<int3>();
            Stack<int3> frontier = new Stack<int3>();
            frontier.Push(chunks.First());

            while (frontier.TryPop(out int3 index))
            {
                foreach (IChunk chunk in districtGenerator.ChunkWaveFunction.Chunks[index].AdjacentChunks)
                {
                    if (chunk is null) continue;
                    
                    if (chunks.Contains(chunk.ChunkIndex) && neighbours.Add(chunk.ChunkIndex))
                    {
                        frontier.Push(chunk.ChunkIndex);
                    }
                }
            }
            
            return chunks.Count == neighbours.Count;
        }

        public bool CheckDistrictRestriction(DistrictType currentType, int3 chunkIndex)
        {
            return currentType switch
            {
                DistrictType.Mine => IsChunkOnCrystal(chunkIndex),
                _ => true,
            };
        }

        private bool IsChunkOnCrystal(int3 chunkIndex)
        {
            if (!chunkGroupTypes.TryGetValue(chunkIndex, out GroundType groundType))
            {
                QueryChunk chunk = districtGenerator.ChunkWaveFunction.Chunks[chunkIndex];
                groundType = RaycastGroundType(chunk.Position);
                chunkGroupTypes.Add(chunkIndex, groundType);
            }

            return groundType is GroundType.Crystal;
        }

        private GroundType RaycastGroundType(Vector3 rayPos)
        {
            Ray ray = new Ray(rayPos + new Vector3(0.1f, 1, 0.1f), Vector3.down);
            Material mat = Chunks.TreeGrower.GetHitMaterial(ray, out _);

            return mat == crystalMaterial ? GroundType.Crystal : GroundType.Grass;
        }

        public void PlacementConfirmed()
        {
            Bounds bounds = GetPositionBounds(spawnedPlacers.Where(x => x.Selected));
            
            HashSet<QueryChunk> chunks = new HashSet<QueryChunk>();
            foreach (QueryChunk chunk in districtGenerator.ChunkWaveFunction.Chunks.Values)
            {
                if (!bounds.Contains(chunk.Position.XZ())) continue;
                
                bool valid = currentType switch
                {
                    DistrictType.TownHall => !districtHandler.IsBuilt(chunk, out DistrictData data) || data.State is TownHallState,
                    _ => !districtHandler.IsBuilt(chunk.ChunkIndex.xz),
                };
                    
                if (valid)
                {
                    chunks.Add(chunk);
                }
            }
            
            HideAnimated();
            districtHandler.BuildDistrict(chunks, currentType);
        }
        
        
        private static Bounds GetBounds(IEnumerable<IChunk> chunks)
        {
            Vector2 min = Vector2.positiveInfinity;
            Vector2 max = Vector2.negativeInfinity;
            
            foreach (IChunk chunk in chunks) 
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
        
        private static Bounds GetBounds(IEnumerable<DistrictPlaceSquare> placers)
        {
            int minX = int.MaxValue, minZ = int.MaxValue, maxX = 0, maxZ = 0;
            foreach (DistrictPlaceSquare placer in placers)
            {
                if (placer.Index.x < minX) minX = placer.Index.x;
                if (placer.Index.x > maxX) maxX = placer.Index.x;
                if (placer.Index.z < minZ) minZ = placer.Index.z;
                if (placer.Index.z > maxZ) maxZ = placer.Index.z;
            }

            return new Bounds
            {
                Min = new Vector2(minX, minZ),
                Max = new Vector2(maxX, maxZ),
            };
        }
        
        private static Bounds GetPositionBounds(IEnumerable<DistrictPlaceSquare> placers)
        {
            Vector2 min = Vector2.positiveInfinity;
            Vector2 max = Vector2.negativeInfinity;

            foreach (DistrictPlaceSquare placer in placers)
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
            Displaying = false;
            spawnedPlacers.Clear();
            selectedPlacers.Clear();

            confirmButton.SetActive(false);
            costText.gameObject.SetActive(false);
        }
        
        private struct Bounds
        {
            public Vector2 Min, Max;

            public bool Contains(Vector2 vec) => 
                   vec.x >= Min.x && vec.x <= Max.x 
                && vec.y >= Min.y && vec.y <= Max.y;
        }
    }*/
}
