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

    private void PlaceObjects()
    {
        for (int i = 0; i < groundObjectDatas.Length; i++)
        {
            PlaceData(groundObjectDatas[i]);
        }
    }

    private void PlaceData(GroundObjectData data)
    {
        int amount = Mathf.RoundToInt(data.SpawnAmountRange.Random());

        for (int i = 0; i < amount; i++)
        {
            Vector3 position = Vector3.zero;
            if (data.SpawnOnGrid)
            {
                position = GetRandomGridIndex(data.ObjectGridSize, out int2 index);
            }

            GameObject spawnedObject = data.Prefab.GetAtPosAndRot<PooledMonoBehaviour>(position, Quaternion.identity).gameObject;
            data.CallSpawnEvent(spawnedObject);
        }
    }

    private Vector3 GetRandomGridIndex(Vector2Int objectGridSize, out int2 index)
    {
        const int y = 0;
        int cellsWidth = BuildingManager.Instance.Cells.GetLength(0);
        int cellsDepth = BuildingManager.Instance.Cells.GetLength(1);
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

                        int2 cellIndex = new int2(xIndex, zIndex);

                        Cell cell = BuildingManager.Instance[cellIndex];
                        Debug.Log("Buildable: " + cell.Buildable);
                        if (!cell.Buildable)
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

                index = new int2((startX + ex) % cellsWidth, (startZ + ze) % cellsDepth);
                Vector3 pos = BuildingManager.Instance[index].Position + new Vector3(1, 0, 1) * BuildingManager.Instance.CellSize;
                return pos;
            }
        }

        Debug.LogError("Could not find SpawnPoint");
        return Vector3.zero;
    }
}
