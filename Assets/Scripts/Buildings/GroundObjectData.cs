using Sirenix.OdinInspector;
using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Ground Object Data")]
public class GroundObjectData : ScriptableObject
{
    public event Action<GameObject> OnObjectSpawned;

    [Title("Prefab")]
    [SerializeField]
    private PooledMonoBehaviour prefab;

    [Title("Spawn Settings", "Position")]
    [SerializeField]
    private bool spawnOnGrid = true;

    [SerializeField]
    [ShowIf(nameof(spawnOnGrid))]
    private Vector2Int objectGridSize;

    [Title("Spawn Settings", "Amount")]
    [SerializeField, MinMaxRange(1, 20)]
    private RangedFloat spawnAmountRange;

    public RangedFloat SpawnAmountRange => spawnAmountRange;
    public Vector2Int ObjectGridSize => objectGridSize;
    public bool SpawnOnGrid => spawnOnGrid;
    public PooledMonoBehaviour Prefab => prefab;

    private void OnValidate()
    {
        spawnAmountRange = new RangedFloat(Mathf.RoundToInt(spawnAmountRange.minValue), Mathf.RoundToInt(spawnAmountRange.maxValue));
    }

    public void CallSpawnEvent(GameObject spawnedObject)
    {
        OnObjectSpawned?.Invoke(spawnedObject);
    }
}
