using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Unity.Mathematics;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Buildings.District
{
    public class UIDistrictPlacementDisplay : SerializedMonoBehaviour
    {
        [Title("Display")]
        [SerializeField]
        private GameObject confirmButton;

        [SerializeField]
        private TextMeshProUGUI costText;
        
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
        private IChunkWaveFunction<Chunk> districtGenerator;

        [Title("GroundType")]
        [SerializeField]
        private Material crystalMaterial;
        
        private Queue<DistrictPlacer> selectedPlacers = new Queue<DistrictPlacer>();
        private List<DistrictPlacer> spawnedPlacers = new List<DistrictPlacer>();
        private readonly Dictionary<int3, GroundType> chunkGroupTypes = new Dictionary<int3, GroundType>();
        
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
        
        private void Display(ChunkWaveFunction<Chunk> chunkWaveFunction)
        {
            Vector3 scale = districtGenerator.ChunkScale * 0.75f;

            Bounds bounds = GetBounds(chunkWaveFunction);

            float maxDistance = Vector2.Distance(bounds.Min, bounds.Max);
            float scaledDelay = maxDelay * Mathf.Clamp01(maxDistance / maxDelayDistance);
            foreach (Chunk chunk in chunkWaveFunction.Chunks.Values)
            {
                if (districtHandler.IsBuilt(chunk)) continue;
                    
                Vector3 pos = chunk.Position + Vector3.up;
                DistrictPlacer spawned = displayPrefab.GetAtPosAndRot<DistrictPlacer>(pos, quaternion.identity);
                spawned.Index = ChunkWaveUtility.GetDistrictIndex3(chunk.Position, districtGenerator.ChunkScale);
                
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
                            && DistrictHandler.CanBuildDistrict(width, depth, currentType)
                            && CheckDistrictRestrictions(currentType, includedChunks);

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

        private bool IsConnected(HashSet<int3> chunks)
        {
            HashSet<int3> neighbours = new HashSet<int3>();
            Stack<int3> frontier = new Stack<int3>();
            frontier.Push(chunks.First());

            while (frontier.TryPop(out int3 index))
            {
                foreach (Chunk chunk in districtGenerator.ChunkWaveFunction.Chunks[index].AdjacentChunks)
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

        public bool CheckDistrictRestrictions(DistrictType currentType, IEnumerable<int3> chunkIndexes)
        {
            return currentType switch
            {
                DistrictType.Mine => AllChunksOnCrystal(chunkIndexes),
                _ => true,
            };
        }

        public bool AllChunksOnCrystal(IEnumerable<int3> chunkIndexes)
        {
            bool valid = true;
            foreach (int3 chunkIndex in chunkIndexes)
            {
                if (!chunkGroupTypes.TryGetValue(chunkIndex, out GroundType groundType))
                {
                    Chunk chunk = districtGenerator.ChunkWaveFunction.Chunks[chunkIndex];
                    groundType = RaycastGroundType(chunk.Position);
                    chunkGroupTypes.Add(chunkIndex, groundType);
                }
                
                if (groundType is not GroundType.Crystal)
                {
                    valid = false;
                    break;
                }
            }

            return valid;
        }

        private GroundType RaycastGroundType(Vector3 rayPos)
        {
            Ray ray = new Ray(rayPos + new Vector3(0.1f, 1, 0.1f), Vector3.down);
            Material mat = Chunks.TreeGrower.GetHitMaterial(ray, out _);

            return mat == crystalMaterial ? GroundType.Crystal : GroundType.Grass;
        }

        public void PlacementConfirmed()
        {
            Bounds bounds = GetPositionBounds(selectedPlacers);
            
            HashSet<Chunk> chunks = new HashSet<Chunk>();
            foreach (Chunk chunk in districtGenerator.ChunkWaveFunction.Chunks.Values)
            {
                if (bounds.Contains(chunk.Position.XZ()) && !districtHandler.IsBuilt(chunk))
                {
                    chunks.Add(chunk);
                }
            }
            
            HideAnimated();
            districtHandler.BuildDistrict(chunks, currentType);
        }
        
        private static Bounds GetBounds(ChunkWaveFunction<Chunk> chunkWaveFunction)
        {
            Vector2 min = Vector2.positiveInfinity;
            Vector2 max = Vector2.negativeInfinity;
            
            foreach (Chunk chunk in chunkWaveFunction.Chunks.Values) 
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
                if (placer.Index.z < minZ) minZ = placer.Index.z;
                if (placer.Index.z > maxZ) maxZ = placer.Index.z;
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
            costText.gameObject.SetActive(false);
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
