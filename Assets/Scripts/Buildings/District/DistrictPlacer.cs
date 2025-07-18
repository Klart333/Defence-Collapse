using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using WaveFunctionCollapse;
using Sirenix.Utilities;
using Unity.Mathematics;
using Gameplay.Money;
using UnityEngine;
using System.Linq;
using InputCamera;
using Gameplay;
using System;
using TMPro;

namespace Buildings.District
{
    public class DistrictPlacer : SerializedMonoBehaviour
    {
        public static bool Placing;

        public event Action OnPlacingCanceled; // Does not mean it failed
        
        [Title("District")]
        [SerializeField]
        private DistrictGenerator districtGenerator;
        
        [SerializeField]
        private BuildingManager buildingGenerator;
        
        [SerializeField]
        private DistrictHandler districtHandler;
        
        [SerializeField]
        private BuildingPlacer buildingPlacer;
        
        [OdinSerialize]
        private Dictionary<DistrictType, PrototypeInfoData> districtInfoData = new Dictionary<DistrictType, PrototypeInfoData>();

        [SerializeField]
        private PooledMonoBehaviour unableToPlacePrefab;
        
        [Title("Cost")]
        [SerializeField]
        private TextMeshProUGUI costText;
        
        [SerializeField]
        private DistrictCostUtility districtCostData;

        [SerializeField]
        private Color affordableColor;
        
        [SerializeField]
        private Color notAffordableColor;
        
        [Title("Debug")]
        [SerializeField]
        private bool verbose = true;
        
        private readonly HashSet<ChunkIndex> buildingIndexes = new HashSet<ChunkIndex>();
        private readonly HashSet<ChunkIndex> builtIndexes = new HashSet<ChunkIndex>();
        
        private int2[,] districtChunkIndexes;
        
        private List<PlaceSquare> hoveredSquares = new List<PlaceSquare>();
        private PooledMonoBehaviour spawnedUnableToPlace;
        private DistrictType districtType;
        private InputManager inputManager;
        private MoneyManager moneyManager;
        private Vector3 offset;
        private Camera cam;

        private int additionalBuildingAmount = 0;
        private bool isPlacementValid;
        private int districtRadius;

        private void OnEnable()
        {
            cam = Camera.main;
            offset = new Vector3(districtGenerator.CellSize.x, 0, districtGenerator.CellSize.z) / -2.0f;

            Events.OnGameReset += OnGameReset;
            UIEvents.OnFocusChanged += CancelPlacement;
            Events.OnDistrictClicked += DistrictClicked;

            GetInput().Forget();
            GetMoney().Forget();
        }

        private async UniTaskVoid GetMoney()
        {
            moneyManager = await MoneyManager.Get();
        }
        
        private async UniTaskVoid GetInput()
        {
            inputManager = await InputManager.Get();
            inputManager.Fire.canceled += FirePerformed;
            inputManager.Cancel.performed += CancelPerformed;
        }

        private void OnDisable()
        {
            Events.OnGameReset -= OnGameReset;
            UIEvents.OnFocusChanged -= CancelPlacement;
            Events.OnDistrictClicked -= DistrictClicked;
            inputManager.Fire.performed -= FirePerformed;
            inputManager.Cancel.performed -= CancelPerformed;
        }

        private void Update()
        {
            if (!Placing || EventSystem.current.IsPointerOverGameObject()) return;
            
            int2[,] lastDistrictChunkIndexes = new int2[districtChunkIndexes.GetLength(0), districtChunkIndexes.GetLength(1)];
            for (int x = 0; x < districtChunkIndexes.GetLength(0); x++)
            for (int y = 0; y < districtChunkIndexes.GetLength(1); y++)
            {
                lastDistrictChunkIndexes[x, y] = districtChunkIndexes[x, y];
            }

            for (int i = 0; i < hoveredSquares.Count; i++)
            {
                hoveredSquares[i].OnHoverExit();
            }
            hoveredSquares.Clear();

            if (!GetChunkIndexes(out bool requireQueryWalls))
            {
                // Invalid, Place some red squares
                SetInvalid();
                return;
            }
            
            if (ChunkIndexesAreIdentical(lastDistrictChunkIndexes, districtChunkIndexes))
            {
                return;
            }

            if (requireQueryWalls)
            {
                if (!QueryWalls())
                {
                    if (verbose)
                    {
                        Debug.Log("Invalid, Wall Query Failed");
                    }
                    SetInvalid();
                    return;
                }
            }
            else
            {
                additionalBuildingAmount = 0;
                buildingGenerator.RevertQuery();
            }

            if (!QueryDistrict())
            {
                if (verbose)
                {
                    Debug.Log("Invalid, District Query Failed");
                }
                SetInvalid();
                return;
            }

            isPlacementValid = true;
            spawnedUnableToPlace?.gameObject.SetActive(false);
            UpdateCost();
        }

        private bool QueryDistrict()
        {
            int height = districtType == DistrictType.TownHall ? 1 : 2;
            Dictionary<ChunkIndex, IBuildable> districts = districtGenerator.Query(districtChunkIndexes, height, districtInfoData[districtType]);
            if (districts == null)
            {
                return false;
            }
            
            bool isDistrictValid = false;
            foreach (IBuildable buildable in districts.Values)
            {
                buildable.ToggleIsBuildableVisual(true, false);
                isDistrictValid |= buildable.MeshRot.MeshIndex != -1;
            }

            return isDistrictValid;
        }

        private bool QueryWalls()
        {
            Dictionary<ChunkIndex, IBuildable> buildings = buildingGenerator.Query(buildingIndexes.ToList(), builtIndexes);
            additionalBuildingAmount = builtIndexes.Count;
            bool isValid = false;
            foreach (IBuildable buildable in buildings.Values)
            {
                buildable.ToggleIsBuildableVisual(true, false);
                if (buildable.MeshRot.MeshIndex != -1)
                {
                    isValid = true;
                }
            }

            return isValid;
        }
        
        private void SetInvalid()
        {
            spawnedUnableToPlace?.gameObject.SetActive(false);

            Vector3 mousePos = Utility.Math.GetGroundIntersectionPoint(cam, Mouse.current.position.ReadValue());
            spawnedUnableToPlace = unableToPlacePrefab.GetAtPosAndRot<PooledMonoBehaviour>(mousePos, Quaternion.identity);

            isPlacementValid = false;
            buildingGenerator.RevertQuery();
            districtGenerator.RevertQuery();
        }
        
        private void UpdateCost()
        {
            int amount = districtHandler.GetDistrictAmount(districtType);
            float cost = districtCostData.GetCost(districtType, amount);
            cost += additionalBuildingAmount * moneyManager.BuildingCost;
            if (cost <= 0) return;

            costText.text = $"{cost:N0}g";

            costText.color = moneyManager.Money >= cost ? affordableColor : notAffordableColor;
            costText.gameObject.SetActive(true);
        }

        private bool ChunkIndexesAreIdentical(int2[,] lastDistrictChunkIndexes, int2[,] districtChunkIndexes)
        {
            for (int x = 0; x < districtChunkIndexes.GetLength(0); x++)
            for (int y = 0; y < districtChunkIndexes.GetLength(1); y++)
            {
                if (!lastDistrictChunkIndexes[x, y].Equals(districtChunkIndexes[x, y]))
                {
                    return false;
                }
            }
            
            return true;
        }

        private bool GetChunkIndexes(out bool requireQueryWalls)
        {
            requireQueryWalls = true;
            buildingIndexes.Clear();
            builtIndexes.Clear();
            Vector3 mousePos = Utility.Math.GetGroundIntersectionPoint(cam, Mouse.current.position.ReadValue());
            for (int x = 0; x < districtRadius; x++)
            for (int z = 0; z < districtRadius; z++)
            {
                if (!GetDistrictIndex(x, z, out Vector3 districtPos))
                {
                    return false;
                }

                ChunkIndex? buildIndex = buildingGenerator.GetIndex(districtPos);// + buildingGenerator.CellSize.XyZ(0) / 2.0f);
                if (!buildIndex.HasValue)
                {
                    if (verbose) Debug.Log("Invalid, Can't find build index");
                    return false;
                }

                buildingIndexes.Add(buildIndex.Value);
                Vector3 buildingCellPosition = buildingGenerator.GetPos(buildIndex.Value);
                
                if (TryGetBuiltIndex(buildingCellPosition, buildIndex.Value, districtPos, out ChunkIndex builtIndex))
                {
                    if (!buildingGenerator.IsBuildable(builtIndex))
                    {
                        if (verbose) Debug.Log($"Invalid, builtIndex: {builtIndex} is not buildable");
                        return false;
                    }
                    
                    if (!buildingPlacer.SpawnedSpawnPlaces.TryGetValue(builtIndex, out var spawnPlace) || spawnPlace.Locked)
                    {
                        if (verbose)
                        {
                            Debug.Log("Invalid, Could not find PlaceSquare at index");
                        }
                        return false;
                    }
                    
                    spawnPlace.OnHover();
                    hoveredSquares.Add(spawnPlace);
                    builtIndexes.Add(builtIndex);
                    buildingIndexes.AddRange(buildingGenerator.GetCellsSurroundingMarchedIndex(builtIndex));
                }
                else
                {
                    if (!buildingGenerator.IsBuildable(builtIndex))
                    {
                        if (verbose) Debug.Log($"Invalid, builtIndex: {builtIndex} is not buildable");
                        return false;
                    }
                }
            
            }
            requireQueryWalls = builtIndexes.Count > 0;
            return true;

            // Returns false if index is already built
            bool TryGetBuiltIndex(Vector3 buildingCellPosition, ChunkIndex buildIndex, Vector3 districtPos, out ChunkIndex builtIndex)
            {
                Vector2 dir = (buildingCellPosition.XZ() - districtPos.XZ()).normalized;
                ChunkIndex? builtIndexNullable = (dir.x, dir.y) switch
                {
                    (x: < 0.1f, y: < 0.1f) => buildIndex,
                    (x: < 0.1f, y: > 0) => buildingGenerator.GetIndex(buildingCellPosition - Vector3.forward * buildingGenerator.CellSize.z / 2.0f),
                    (x: > 0, y: < 0.1f) => buildingGenerator.GetIndex(buildingCellPosition - Vector3.right * buildingGenerator.CellSize.z / 2.0f),
                    (x: > 0, y: > 0) => buildingGenerator.GetIndex(buildingCellPosition - buildingGenerator.CellSize / 2.0f),
                    _ => throw new ArgumentOutOfRangeException()
                };

                if (!builtIndexNullable.HasValue)
                {
                    builtIndex = default;
                    return false;
                }
                
                builtIndex = builtIndexNullable.Value;
                if (buildingGenerator.ChunkWaveFunction.Chunks[builtIndex.Index].BuiltCells[builtIndex.CellIndex.x, builtIndex.CellIndex.y, builtIndex.CellIndex.z] 
                    && !buildingGenerator.ChunkWaveFunction.Chunks[builtIndex.Index].QueryBuiltCells.Contains(builtIndex.CellIndex))
                {
                    // BuiltIndex is alread built
                    return false;
                }
                
                return true;
            }

            bool GetDistrictIndex(int x, int z, out Vector3 districtPos)
            {
                Vector3 pos = mousePos + new Vector3(x * districtGenerator.ChunkScale.x, 0, z * districtGenerator.ChunkScale.z);
                districtPos = pos + offset - districtGenerator.CellSize.XyZ() * (districtRadius % 2 == 0 ? 0.5f : 0.25f);
                districtChunkIndexes[x, z] = ChunkWaveUtility.GetDistrictIndex2(districtPos, districtGenerator.ChunkScale);
                if (districtHandler.IsBuilt(districtChunkIndexes[x, z]))
                {
                    if (verbose) Debug.Log("Invalid, District index is already built");
                    return false;
                }
                
                districtPos = ChunkWaveUtility.GetPosition(districtChunkIndexes[x, z].XyZ(0), districtGenerator.ChunkScale);
                ChunkIndex? buildingIndex = buildingGenerator.GetIndex(districtPos + buildingGenerator.CellSize / 2.0f);
                if (!buildingIndex.HasValue)
                {
                    if (verbose) Debug.Log("Invalid, District index is out of bounds");
                    return false;
                }
                
                GroundType groundType = buildingGenerator.ChunkWaveFunction.Chunks[buildingIndex.Value.Index].GroundTypes[buildingIndex.Value.CellIndex.x, buildingIndex.Value.CellIndex.y, buildingIndex.Value.CellIndex.z];
                if (!IsBuildable(groundType))
                {
                    if (verbose) Debug.Log($"Invalid, The groundType: {groundType} does not match the district's restrictions");
                    return false;
                }

                return true;
            }
        }

        private void DistrictClicked(DistrictType districtType, int radius)
        {
            UIEvents.OnFocusChanged?.Invoke();
            
            buildingPlacer.ToggleSpawnPlaces(true);
            this.districtType = districtType;
            districtChunkIndexes = new int2[radius, radius];
            districtRadius = radius;
            Placing = true;
            isPlacementValid = false;
        }

        private bool IsBuildable(GroundType groundType)
        {
            return districtType switch
            {
                DistrictType.Mine => groundType is GroundType.Crystal,
                _ => groundType is GroundType.Grass or GroundType.Crystal
            };
        }

        private void CancelPerformed(InputAction.CallbackContext obj)
        {
            if (Placing)
            {
                CancelPlacement();
            }
        }

        private void CancelPlacement()
        {
            buildingGenerator.RevertQuery();
            districtGenerator.RevertQuery();
            costText.gameObject.SetActive(false);

            Placing = false;
            isPlacementValid = false;
            spawnedUnableToPlace?.gameObject.SetActive(false);
            buildingPlacer.ToggleSpawnPlaces(false);

            OnPlacingCanceled?.Invoke();
        }

        private void FirePerformed(InputAction.CallbackContext obj)
        {
            if (!Placing || CameraController.IsDragging || !isPlacementValid)
            {
                return;
            }

            PlaceDistrict();
        }

        private void PlaceDistrict()
        {
            int amount = districtHandler.GetDistrictAmount(districtType);
            if (!moneyManager.CanPurchase(districtType, amount, additionalBuildingAmount, out float cost))
            {
                return;
            }

            moneyManager.RemoveMoney(cost);
            
            PrototypeInfoData protInfo = districtInfoData[districtType];
            HashSet<QueryChunk> chunks = districtGenerator.QueriedChunks.Where(x => x.PrototypeInfoData == protInfo).ToHashSet();
            districtHandler.AddBuiltDistrict(chunks, districtType);

            if (buildingGenerator.QueryBuiltIndexes != null)
            {
                foreach (ChunkIndex chunkIndex in buildingGenerator.QueryBuiltIndexes)
                {
                    if (buildingPlacer.SpawnedSpawnPlaces.TryGetValue(chunkIndex, out PlaceSquare spawnPlace))
                    {
                        spawnPlace.OnPlaced();
                    }
                }
            }
            
            districtGenerator.Place();

            bool shouldUpdate = buildingGenerator.QuerySpawnedBuildings.Count <= 0;
            buildingGenerator.Place(); 
            
            if (shouldUpdate)
            {
                UpdateBuildingIndexes();
            }
            
            costText.gameObject.SetActive(false);
            
            if (districtType == DistrictType.TownHall)
            {
                CancelPlacement();
            }
            else
            {
                SetInvalid();
            }

            void UpdateBuildingIndexes()
            {
                List<IBuildable> buildables = new List<IBuildable>();
                foreach (ChunkIndex chunkIndex in buildingIndexes)
                {
                    buildables.Add(buildingGenerator.SpawnedMeshes[chunkIndex]);
                }
                districtGenerator.UpdateChunksAtBuildables(buildables);
            }
        }

        private void OnGameReset()
        {
            Placing = false;
        }
        
        #region Debug

        private void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying || districtChunkIndexes == null) return;
#endif

            foreach (int2 chunkIndex in districtChunkIndexes)
            {
                Vector3 districtPos = ChunkWaveUtility.GetPosition(chunkIndex.XyZ(0), districtGenerator.ChunkScale) - offset;
                Gizmos.color = Color.blue; 
                Gizmos.DrawWireCube(districtPos + districtGenerator.CellSize.XyZ() * 0.5f, districtGenerator.ChunkScale * 0.95f);
            }
            
            foreach (QueryMarchedChunk chunk in buildingGenerator.ChunkWaveFunction.Chunks.Values)
            {
                for (int y = 0; y < chunk.Cells.GetLength(2); y++)
                {
                    for (int x = 0; x < chunk.Cells.GetLength(0); x++)
                    {
                        Vector3 pos = chunk.Cells[x, 0, y].Position;
                        ChunkIndex index = new ChunkIndex(chunk.ChunkIndex, new int3(x, 0, y));
                        Gizmos.color = builtIndexes.Contains(index) 
                            ? Color.magenta
                            : buildingIndexes.Contains(index)
                                ? Color.red 
                                : Color.black;
                        Gizmos.DrawWireCube(pos, buildingGenerator.CellSize * 0.95f);
                    }
                }
            }
        
        }

        #endregion
    }
}