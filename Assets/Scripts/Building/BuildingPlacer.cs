﻿using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using System.Linq;
using DG.Tweening;
using Unity.Mathematics;
using WaveFunctionCollapse;

public class BuildingPlacer : MonoBehaviour
{
    [Title("Placing")]
    [SerializeField]
    private LayerMask layerMask;

    [SerializeField]
    private PooledMonoBehaviour unableToPlacePrefab;

    [SerializeField]
    private PlaceSquare placeSquarePrefab;

    private readonly Dictionary<ChunkIndex, PlaceSquare> spawnedSpawnPlaces = new Dictionary<ChunkIndex, PlaceSquare>();
    private readonly List<PooledMonoBehaviour> spawnedUnablePlaces = new List<PooledMonoBehaviour>();

    private GroundGenerator groundGenerator;
    private Vector3 targetScale;
    
    private bool manualCancel;
    private bool Canceled => InputManager.Instance.Cancel.WasPerformedThisFrame() || manualCancel;
    public ChunkIndex? SquareIndex { get; set; }
    public int SpawnSquareIndex { get; set; }

    private async void OnEnable()
    {
        Events.OnBuildingCanceled += OnBuildingCanceled;
        groundGenerator = FindFirstObjectByType<GroundGenerator>();
        
        Events.OnBuildingDestroyed += OnBuildingDestroyed;
        
        await UniTask.WaitUntil(() => BuildingManager.Instance != null);
        BuildingManager.Instance.OnLoaded += InitializeSpawnPlaces;
    }
    
    private void OnDisable()
    {
        Events.OnBuildingCanceled -= OnBuildingCanceled;
        BuildingManager.Instance.OnLoaded -= InitializeSpawnPlaces;
        Events.OnBuildingDestroyed -= OnBuildingDestroyed;
    }

    private void Start()
    {
        Events.OnBuildingPurchased += BuildingPurchased;
    }
    
    private void InitializeSpawnPlaces(QueryMarchedChunk chunk)
    {
        targetScale = groundGenerator.ChunkWaveFunction.GridScale.MultiplyByAxis(BuildingManager.Instance.ChunkWaveFunction.GridScale);
        for (int x = 0; x < chunk.Width - 1; x++)
        {
            for (int z = 0; z < chunk.Depth - 1; z++)
            {
                ChunkIndex chunkIndex = new ChunkIndex(chunk.ChunkIndex, new int3(x, 0, z));
                if (spawnedSpawnPlaces.ContainsKey(chunkIndex) 
                           || !chunk.Cells[x,     0, z].Buildable
                           || !chunk.Cells[x + 1, 0, z].Buildable
                           || !chunk.Cells[x,     0, z + 1].Buildable
                           || !chunk.Cells[x + 1, 0, z + 1].Buildable) continue;

                Vector3 pos = chunk.Cells[x, 0, z].Position + new Vector3(targetScale.x / 2.0f, 0.1f, targetScale.z / 2.0f);
                SpawnSquare(pos, chunkIndex);
            }
        }

        Dictionary<int3,QueryMarchedChunk> chunks = BuildingManager.Instance.ChunkWaveFunction.Chunks;
        CheckIntersection(chunk);
        if (chunks.TryGetValue(chunk.ChunkIndex - new int3(0, 0, 1), out QueryMarchedChunk bottomChunk))
            CheckIntersection(bottomChunk);
        if (chunks.TryGetValue(chunk.ChunkIndex - new int3(1, 0, 0), out QueryMarchedChunk leftChunk))
            CheckIntersection(leftChunk);
        if (chunks.TryGetValue(chunk.ChunkIndex - new int3(1, 0, 1), out QueryMarchedChunk bottomLeftChunk))
            CheckIntersection(bottomLeftChunk); 

        void CheckIntersection(QueryMarchedChunk chunk)
        {
            // Top
            if (chunks.TryGetValue(chunk.ChunkIndex + new int3(0, 0, 1), out QueryMarchedChunk topChunk))
            {
                int z = chunk.Depth - 1;
                for (int x = 0; x < chunk.Width - 1; x++)
                {
                    ChunkIndex chunkIndex = new ChunkIndex(chunk.ChunkIndex, new int3(x, 0, z));
                    if (spawnedSpawnPlaces.ContainsKey(chunkIndex) 
                        || !chunk.Cells[x,     0, z].Buildable
                        || !chunk.Cells[x + 1, 0, z].Buildable
                        || !topChunk.Cells[x,     0, 0].Buildable
                        || !topChunk.Cells[x + 1, 0, 0].Buildable) continue;

                    Vector3 pos = chunk.Cells[x, 0, z].Position + new Vector3(targetScale.x / 2.0f, 0.1f, targetScale.z / 2.0f);
                    SpawnSquare(pos, chunkIndex);
                }
            }

            // Right
            if (chunks.TryGetValue(chunk.ChunkIndex + new int3(1, 0, 0), out QueryMarchedChunk rightChunk))
            {
                int x = chunk.Width - 1;
                for (int z = 0; z < chunk.Depth - 1; z++)
                {
                    ChunkIndex chunkIndex = new ChunkIndex(chunk.ChunkIndex, new int3(x, 0, z));
                    if (spawnedSpawnPlaces.ContainsKey(chunkIndex) 
                        || !chunk.Cells[x, 0, z].Buildable
                        || !chunk.Cells[x, 0, z + 1].Buildable
                        || !rightChunk.Cells[0, 0, z].Buildable
                        || !rightChunk.Cells[0, 0, z + 1].Buildable) continue;

                    Vector3 pos = chunk.Cells[x, 0, z].Position + new Vector3(targetScale.x / 2.0f, 0.1f, targetScale.z / 2.0f);
                    SpawnSquare(pos, chunkIndex);
                }
            }
            
            // Top Right
            if (chunks.TryGetValue(chunk.ChunkIndex + new int3(1, 0, 1), out QueryMarchedChunk topRightChunk) 
                && topChunk != null && rightChunk != null)
            {
                int x = chunk.Width - 1;
                int z = chunk.Depth - 1;
                ChunkIndex chunkIndex = new ChunkIndex(chunk.ChunkIndex, new int3(x, 0, z));
                if (spawnedSpawnPlaces.ContainsKey(chunkIndex) 
                    || !chunk.Cells[x, 0, z].Buildable
                    || !rightChunk.Cells[0, 0, z].Buildable
                    || !topChunk.Cells[x, 0, 0].Buildable
                    || !topRightChunk.Cells[0, 0, 0].Buildable) return;

                Vector3 pos = chunk.Cells[x, 0, z].Position + new Vector3(targetScale.x / 2.0f, 0.1f, targetScale.z / 2.0f);
                SpawnSquare(pos, chunkIndex);
            }
        }
        
        void SpawnSquare(Vector3 pos, ChunkIndex chunkIndex)
        {
            PlaceSquare placeSquare = Instantiate(placeSquarePrefab, pos, placeSquarePrefab.transform.rotation);
            placeSquare.Placer = this;
            placeSquare.Index = chunkIndex;
            placeSquare.transform.localScale = targetScale * 0.95f;
            placeSquare.transform.SetParent(transform, true);
            placeSquare.gameObject.SetActive(false);
            placeSquare.SquareIndex = spawnedSpawnPlaces.Count;
            spawnedSpawnPlaces.Add(chunkIndex, placeSquare);
        }
    }
    
    private void OnBuildingCanceled()
    {
        manualCancel = true;
    }

    private void BuildingPurchased(BuildingType buildingType)
    {
        PlacingTower(buildingType).Forget(Debug.LogError);
    }

    private async UniTask PlacingTower(BuildingType type)
    {
        manualCancel = false;
        
        ToggleSpawnPlaces(true);

        ChunkIndex queryIndex = new ChunkIndex();
        Dictionary<ChunkIndex, IBuildable> buildables = new Dictionary<ChunkIndex, IBuildable>();
        while (!Canceled)
        {
            await UniTask.Yield();

            if (!SquareIndex.HasValue) continue;

            if (queryIndex.Equals(SquareIndex.Value))
            {
                if (buildables.Count > 0 && InputManager.Instance.Fire.WasPerformedThisFrame())
                {
                    PlaceBuilding();
                }
                continue;
            }
            
            DisablePlaces();

            BuildingManager.Instance.RevertQuery();
            await UniTask.NextFrame();
            if (!SquareIndex.HasValue) continue;

            queryIndex = SquareIndex.Value;
            buildables = BuildingManager.Instance.Query(queryIndex, type);
            
            foreach (IBuildable item in buildables.Values)
            {
                item.ToggleIsBuildableVisual(true);
            }
            
            if (buildables.Count == 0) 
            {
                ShowUnablePlaces(BuildingManager.Instance.GetCellsToCollapse(queryIndex).Select(x => BuildingManager.Instance.GetPos(x) + Vector3.up * BuildingManager.Instance.ChunkScale.y / 2.0f).ToList());
                continue;
            }

            if (!InputManager.Instance.Fire.WasPerformedThisFrame()) continue;

            PlaceBuilding();
        }

        if (Canceled)
        {
            SquareIndex = null;
            ToggleSpawnPlaces(false);
            DisablePlaces();
            BuildingManager.Instance.RevertQuery();

            if (!manualCancel)
            {
                Events.OnBuildingCanceled?.Invoke();
            }
        }
    }

    private void ToggleSpawnPlaces(bool enabled)
    {
        foreach (PlaceSquare placeSquare in spawnedSpawnPlaces.Values)
        {
            if (enabled)
            {
                placeSquare.gameObject.SetActive(true);
                placeSquare.transform.DOKill();
                placeSquare.transform.DOScale(targetScale * 0.95f, 0.5f).SetEase(Ease.OutCirc);
            }
            else
            {
                placeSquare.transform.DOKill();
                placeSquare.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.OutCirc).OnComplete(() =>
                {
                    placeSquare.gameObject.SetActive(false);
                });
                
            }
        }
    }
    
    private void PlaceBuilding()
    {
        if (!SquareIndex.HasValue)
        {
            return;
        }
        
        spawnedSpawnPlaces[SquareIndex.Value].OnPlaced();
        BuildingManager.Instance.Place();
    }
    
    private void OnBuildingDestroyed(Building building)
    {
        if (spawnedSpawnPlaces.TryGetValue(building.Index, out PlaceSquare square))
        {
            square.UnPlaced();
        }
    }

    private void ShowUnablePlaces(List<Vector3> positions)
    {
        for (int i = 0; i < positions.Count; i++)
        {
            spawnedUnablePlaces.Add(unableToPlacePrefab.GetAtPosAndRot<PooledMonoBehaviour>(positions[i], Quaternion.identity));
        }
    }

    private void DisablePlaces()
    {
        for (int i = 0; i < spawnedUnablePlaces.Count; i++)
        {
            spawnedUnablePlaces[i].gameObject.SetActive(false);
        }
    }
}

public enum BuildingType
{
    Building,
    Path
}