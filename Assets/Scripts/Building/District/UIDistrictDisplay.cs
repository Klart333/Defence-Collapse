using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using DG.Tweening;
using UnityEngine;

namespace Buildings.District
{
    public class UIDistrictDisplay : SerializedMonoBehaviour
{
    [Title("Display")]
    [SerializeField]
    private PooledMonoBehaviour displayPrefab;

    [SerializeField]
    private float maxDelay = 2f;

    [SerializeField]
    private float maxDelayDistance = 20;
    
    [Title("District")]
    [SerializeField]
    private DistrictHandler districtHandler;
    
    [SerializeField]
    private IChunkWaveFunction districtGenerator;
    
    private List<PooledMonoBehaviour> spawnedObjects = new List<PooledMonoBehaviour>();
    
    private void OnEnable()
    {
        Events.OnDistrictClicked += OnDistrictClicked;
    }

    private void OnDisable()
    {
        Events.OnDistrictClicked -= OnDistrictClicked;
    }

    private void OnDistrictClicked(DistrictType districtType)
    {
        Hide();
        
        // Display Disctrict UI
        Display(districtGenerator.ChunkWaveFunction);
    }
    
    private void Display(ChunkWaveFunction chunkWaveFunction)
    {
        Vector3 scale = chunkWaveFunction.GridScale * 0.75f;

        Vector2 min = Vector2.positiveInfinity;
        Vector2 max = Vector2.negativeInfinity;
        
        foreach (Chunk chunk in chunkWaveFunction.Chunks) // Get Bounds
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
                var spawned = displayPrefab.GetAtPosAndRot<PooledMonoBehaviour>(pos, quaternion.identity);
                spawned.transform.localScale = Vector3.zero;
                
                float delay = scaledDelay * (Vector2.Distance(min, cell.Position.XZ()) / maxDistance);
                spawned.transform.DOScale(scale, 0.5f).SetEase(Ease.OutBounce).SetDelay(delay);
                spawnedObjects.Add(spawned);
            }
        }
    }

    public void Hide()
    {
        for (int i = 0; i < spawnedObjects.Count; i++)
        {
            spawnedObjects[i].gameObject.SetActive(false);
        }
        spawnedObjects.Clear();
    }
}
}
