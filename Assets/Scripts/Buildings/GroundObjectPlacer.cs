using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using WaveFunctionCollapse;

public class GroundObjectPlacer : MonoBehaviour
{
    [Title("Data")]
    [SerializeField]
    private GroundObjectData[] groundObjectDatas;

    private WaveFunction waveFunction;

    private async void OnEnable()
    {
        await UniTask.WaitWhile(() => BuildingManager.Instance == null);

        BuildingManager.Instance.OnLoaded += PlaceObjects;
    }

    private void OnDisable()
    {
        BuildingManager.Instance.OnLoaded -= PlaceObjects;
    }

    private void PlaceObjects(QueryMarchedChunk chunk) // MAKE WORK WITH CHUNKS!!!
    {
        for (int i = 0; i < groundObjectDatas.Length; i++)
        {
            PlaceData(groundObjectDatas[i]);
        }
    }

    private void PlaceData(GroundObjectData data)
    {
        int amount = Mathf.RoundToInt(data.SpawnAmountRange.Random());
        int3[] keys = Array.Empty<int3>();
        if (data.SpawnOnGrid)
        {
            keys = BuildingManager.Instance.ChunkWaveFunction.Chunks.Keys.ToArray();
        }

        for (int i = 0; i < amount; i++)
        {
            Vector3 position = Vector3.zero;
            if (data.SpawnOnGrid)
            {
                position = GetRandomGridIndex(data.ObjectGridSize, keys, out ChunkIndex index);
            }

            GameObject spawnedObject = data.Prefab.GetAtPosAndRot<PooledMonoBehaviour>(position, Quaternion.identity).gameObject;
            data.CallSpawnEvent(spawnedObject);
        }
    }

    private Vector3 GetRandomGridIndex(Vector2Int objectGridSize, int3[] keys, out ChunkIndex index)
    {
        int3 chunkIndex = keys[UnityEngine.Random.Range(0, keys.Length)];
        QueryMarchedChunk chunk = BuildingManager.Instance.ChunkWaveFunction.Chunks[chunkIndex];
        
        const int y = 0;
        int cellsWidth = chunk.Width;
        int cellsDepth = chunk.Depth;
        int startX = UnityEngine.Random.Range(0, cellsWidth);
        int startZ = UnityEngine.Random.Range(0, cellsDepth);
        index = default;

        for (int ex = 0; ex < cellsWidth; ex++)
        {
            for (int ze = 0; ze < cellsDepth; ze++)
            {
                bool valid = true;
                for (int x = 0; x < objectGridSize.x && valid; x++)
                {
                    for (int z = 0; z < objectGridSize.y; z++)
                    {
                        int xIndex = (startX + ex) % cellsWidth + x;
                        int zIndex = (startZ + ze) % cellsDepth + z;
                        if (xIndex >= cellsWidth || zIndex >= cellsDepth)
                        {
                            valid = false;
                            break;
                        }
                    }
                }
                
                if (!valid)
                {
                    continue;
                }

                index = new ChunkIndex(chunkIndex, new int3((startX + ex) % cellsWidth, 0, (startZ + ze) % cellsDepth) );
                Vector3 pos = chunk[index.CellIndex].Position + new Vector3(BuildingManager.Instance.ChunkScale.x, 0, BuildingManager.Instance.ChunkScale.z);
                return pos;
            }
        }

        Debug.Log("Could not find SpawnPoint");
        return Vector3.zero;
    }
}
