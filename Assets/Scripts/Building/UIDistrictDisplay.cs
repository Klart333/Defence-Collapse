using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

public class UIDistrictDisplay : SerializedMonoBehaviour
{
    [Title("Display")]
    [SerializeField]
    private PooledMonoBehaviour displayPrefab;
    
    [Title("District")]
    [SerializeField]
    private BuildingHandler buildingHandler;
    
    [SerializeField]
    private IChunkWaveFunction districtGenerator;
    
    private List<PooledMonoBehaviour> spawnedObjects = new List<PooledMonoBehaviour>();
    
    private bool displaying;
    
    private void OnEnable()
    {
        Events.OnDistrictClicked += OnDistrictClicked;
    }

    private void OnDisable()
    {
        Events.OnDistrictClicked -= OnDistrictClicked;
    }

    private void OnDistrictClicked()
    {
        Hide();
        
        // Display Disctrict UI
        Display(districtGenerator.ChunkWaveFunction).Forget(Debug.LogError);
    }
    
    private async UniTask Display(ChunkWaveFunction chunkWaveFunction)
    {
        if (displaying) return;
        
        displaying = true;
        Vector3 scale = chunkWaveFunction.GridScale * 0.75f;

        const float targetAwaits = 100;
        float totalCells = chunkWaveFunction.Chunks.Count * 4;
        int interval = Mathf.CeilToInt(totalCells / targetAwaits);
        
        int n = 0;
        foreach (Chunk chunk in chunkWaveFunction.Chunks)
        {
            for (int x = 0; x < chunk.Cells.GetLength(0); x++)
            for (int z = 0; z < chunk.Cells.GetLength(2); z++)
            {
                Cell cell = chunk.Cells[x, 0, z];
                Vector3 pos = cell.Position + Vector3.up;
                var spawned = displayPrefab.GetAtPosAndRot<PooledMonoBehaviour>(pos, quaternion.identity);
                spawned.transform.localScale = Vector3.zero;
                spawned.transform.DOScale(scale, 0.5f).SetEase(Ease.OutBounce);
                spawnedObjects.Add(spawned);

                if (++n >= interval)
                {
                    n = 0;
                    await UniTask.Yield();
                }
            }
        }
    }

    public void Hide()
    {
        displaying = false;
        for (int i = 0; i < spawnedObjects.Count; i++)
        {
            spawnedObjects[i].gameObject.SetActive(false);
        }
        spawnedObjects.Clear();
    }
}