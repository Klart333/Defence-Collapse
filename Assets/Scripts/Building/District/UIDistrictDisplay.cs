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
            Vector3 scale = chunkWaveFunction.GridScale * 0.75f;

            GetBounds(chunkWaveFunction, out Vector2 min, out Vector2 max);

            float maxDistance = Vector2.Distance(min, max);
            float scaledDelay = maxDelay * Mathf.Clamp01(maxDistance / maxDelayDistance);
            foreach (Chunk chunk in chunkWaveFunction.Chunks)
            {
                for (int x = 0; x < chunk.Cells.GetLength(0); x++)
                for (int z = 0; z < chunk.Cells.GetLength(2); z++)
                {
                    Cell cell = chunk.Cells[x, 0, z];
                    if (districtHandler.IsBuilt(cell)) continue;
                    
                    Vector3 pos = cell.Position + Vector3.up;
                    DistrictPlacer spawned = displayPrefab.GetAtPosAndRot<DistrictPlacer>(pos, quaternion.identity);
                    spawned.Index = districtHandler.GetDistrictIndex(pos);
                    
                    spawned.transform.localScale = Vector3.zero;
                    float delay = scaledDelay * (Vector2.Distance(min, cell.Position.XZ()) / maxDistance);
                    spawned.transform.DOScale(scale, 0.5f).SetEase(Ease.OutBounce).SetDelay(delay);
                    spawnedPlacers.Add(spawned);
                    
                    spawned.OnSelected += PlacerOnOnSelected;
                }
            }
        }
        
        private static void GetBounds(ChunkWaveFunction chunkWaveFunction, out Vector2 min, out Vector2 max)
        {
            min = Vector2.positiveInfinity;
            max = Vector2.negativeInfinity;
            
            foreach (Chunk chunk in chunkWaveFunction.Chunks) 
            {
                for (int x = 0; x < chunk.Cells.GetLength(0); x++)
                for (int z = 0; z < chunk.Cells.GetLength(2); z++)
                {
                    Vector3 pos = chunk.Cells[x, 0, z].Position;
                    if (pos.x < min.x) min.x = pos.x;
                    if (pos.z < min.y) min.y = pos.z;
                    if (pos.x > max.x) max.x = pos.x;
                    if (pos.z > max.y) max.y = pos.z;
                }
            }
        }

        private void PlacerOnOnSelected(DistrictPlacer selectedPlacer)
        {
            selectedPlacers.Enqueue(selectedPlacer);
            selectedPlacer.SetSelected();

            if (selectedPlacers.Count == 1) return;
            
            if (selectedPlacers.Count > 2)
            {
                var removedPlacer = selectedPlacers.Dequeue();
                removedPlacer.Unselect();
            }

            int minX = int.MaxValue, minZ = int.MaxValue, maxX = 0, maxZ = 0;
            foreach (DistrictPlacer placer in selectedPlacers)
            {
                if (placer.Index.x < minX) minX = placer.Index.x;
                if (placer.Index.x > maxX) maxX = placer.Index.x;
                if (placer.Index.y < minZ) minZ = placer.Index.y;
                if (placer.Index.y > maxZ) maxZ = placer.Index.y;
            }
            
            if (DistrictHandler.CanBuildDistrict(maxX - minX + 1, maxZ - minZ + 1, currentType))
            {
                Debug.Log("Valid");
                confirmButton.SetActive(true);
            }
            else
            {
                confirmButton.SetActive(false);
            }

            for (int i = 0; i < spawnedPlacers.Count; i++)
            {
                int2 index = spawnedPlacers[i].Index;
                if (index.x >= minX && index.x <= maxX && index.y >= minZ && index.y <= maxZ)
                {
                    spawnedPlacers[i].SetSelected();
                }
                else
                {
                    spawnedPlacers[i].Unselect();
                }
            }
        }

        public void Hide()
        {
            for (int i = 0; i < spawnedPlacers.Count; i++)
            {
                spawnedPlacers[i].gameObject.SetActive(false);
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
            }
            
            Clear();
        }

        private void Clear()
        {
            spawnedPlacers.Clear();
            selectedPlacers.Clear();

            confirmButton.SetActive(false);
        }
    }
}
