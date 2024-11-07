using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Ground Object Data")]
public class GroundObjectData : ScriptableObject
{
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
}
