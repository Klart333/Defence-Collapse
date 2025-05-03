using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine.InputSystem;
using WaveFunctionCollapse;
using Unity.Mathematics;
using UnityEngine;

namespace Buildings.District
{
    public class DistrictPlacer : SerializedMonoBehaviour
    {
        public static bool Placing;
        
        [SerializeField]
        private DistrictGenerator districtGenerator;
        
        [SerializeField]
        private BuildingManager buildingGenerator;

        [SerializeField]
        private Dictionary<DistrictType, PrototypeInfoData> districtInfoData = new Dictionary<DistrictType, PrototypeInfoData>();
        
        private readonly HashSet<ChunkIndex> buildingIndexes = new HashSet<ChunkIndex>();
        private readonly HashSet<ChunkIndex> builtIndexes = new HashSet<ChunkIndex>();
        
        private int2[,] districtChunkIndexes;
        
        private DistrictType districtType;
        private InputManager inputManager;
        private Vector3 offset;
        private Camera cam;

        private bool isPlacementValid;
        private int districtRadius;

        private void OnEnable()
        {
            cam = Camera.main;
            offset = new Vector3(districtGenerator.CellSize.x, 0, districtGenerator.CellSize.z) / -2.0f;

            Events.OnDistrictClicked += DistrictClicked;

            GetInput().Forget();
        }

        private async UniTaskVoid GetInput()
        {
            inputManager = await InputManager.Get();
            inputManager.Fire.performed += FirePerformed;
            inputManager.Cancel.performed += CancelPerformed;
        }

        private void OnDisable()
        {
            Events.OnDistrictClicked -= DistrictClicked;
            inputManager.Fire.performed -= FirePerformed;
            inputManager.Cancel.performed -= CancelPerformed;
        }

        private void Update()
        {
            if (!Placing) return;

            GetChunkIndexes();
            
            Dictionary<ChunkIndex, IBuildable> buildings = buildingGenerator.Query(buildingIndexes.ToList(), builtIndexes);
            foreach (IBuildable buildable in buildings.Values)
            {
                buildable.ToggleIsBuildableVisual(true, false);
            }

            if (buildings.Count <= 0)
            {
                isPlacementValid = false;
                return;
            }

            districtGenerator.Query(districtChunkIndexes, districtInfoData[districtType]);
        }

        private void GetChunkIndexes()
        {
            buildingIndexes.Clear();
            builtIndexes.Clear();
            Vector3 mousePos = Math.GetGroundIntersectionPoint(cam, Mouse.current.position.ReadValue());
            for (int x = 0; x < districtRadius; x++)
            {
                for (int z = 0; z < districtRadius; z++)
                {
                    Vector3 districtPos = GetDistrictIndex(x, z);

                    ChunkIndex? buildIndex = buildingGenerator.GetIndex(districtPos + buildingGenerator.CellSize * 0.5f);
                    if (!buildIndex.HasValue)
                    {
                        continue;
                    }

                    buildingIndexes.Add(buildIndex.Value);
                    Vector3 buildingCellPosition = GetBuiltIndexes(buildIndex.Value, districtPos);

                    GetAdjacentCells(buildingCellPosition);
                }
            }

            Vector3 GetBuiltIndexes(ChunkIndex buildIndex, Vector3 districtPos)
            {
                Vector3 buildingCellPosition = buildingGenerator.GetPos(buildIndex);
                Vector2 dir = (buildingCellPosition.XZ() - districtPos.XZ()).normalized;
                builtIndexes.Add((dir.x, dir.y) switch
                {
                    (x: < 0.1f, y: < 0.1f) => buildIndex,
                    (x: < 0.1f, y: > 0) => buildingGenerator.GetIndex(buildingCellPosition - Vector3.forward * buildingGenerator.CellSize.z).GetValueOrDefault(),
                    (x: > 0, y: < 0.1f) => buildingGenerator.GetIndex(buildingCellPosition - Vector3.right * buildingGenerator.CellSize.z).GetValueOrDefault(),
                    (x: > 0, y: > 0) => buildingGenerator.GetIndex(buildingCellPosition - buildingGenerator.CellSize).GetValueOrDefault(),
                    _ => throw new ArgumentOutOfRangeException()
                });
                return buildingCellPosition;
            }

            Vector3 GetDistrictIndex(int x, int z)
            {
                Vector3 pos = mousePos + new Vector3(x * districtGenerator.ChunkScale.x, 0, z * districtGenerator.ChunkScale.z);
                Vector3 districtPos = pos + offset;//+ districtGenerator.CellSize.XyZ() * (districtRadius % 2 == 0 ? 0 : -.25f);
                districtChunkIndexes[x, z] = ChunkWaveUtility.GetDistrictIndex2(districtPos, districtGenerator.ChunkScale);
                districtPos = ChunkWaveUtility.GetPosition(districtChunkIndexes[x, z].XyZ(0), districtGenerator.ChunkScale);
                return districtPos;
            }
            
            void GetAdjacentCells(Vector3 buildingCellPosition)
            {
                for (int i = 0; i < WaveFunctionUtility.DiagonalNeighbourDirections.Length; i++)
                {
                    Vector3 neighbourPos = buildingCellPosition + buildingGenerator.CellSize.MultiplyByAxis(WaveFunctionUtility.DiagonalNeighbourDirections[i].XyZ(0));
                    ChunkIndex? neighbourIndex = buildingGenerator.GetIndex(neighbourPos);
                    if (!neighbourIndex.HasValue)
                    {
                        Debug.LogError("Couldn't find building index"); // Handle exception
                        continue;
                    }
                        
                    buildingIndexes.Add(neighbourIndex.Value);
                }
            }
        }

        private void DistrictClicked(DistrictType districtType, int radius)
        {
            UIEvents.OnFocusChanged?.Invoke();
            
            districtChunkIndexes = new int2[radius, radius];
            districtRadius = radius;
            Placing = true;
        }

        private void CancelPerformed(InputAction.CallbackContext obj)
        {
            Placing = false;
            buildingGenerator.RevertQuery();
        }

        private void FirePerformed(InputAction.CallbackContext obj)
        {
            buildingGenerator.Place();
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
                        Gizmos.DrawWireCube(pos + buildingGenerator.CellSize.XyZ() * 0.5f, buildingGenerator.CellSize * 0.95f);
                    }
                }
            }
        
        }

        #endregion
    }
}