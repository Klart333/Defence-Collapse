using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Buildable Cost Data", menuName = "Building/Cost Data"), InlineEditor]
public class BuildableCostData : SerializedScriptableObject
{
    [Title("Costs")]
    [SerializeField]
    public Dictionary<BuildingType, int> BuildingCosts { get; private set; }

    public int GetCost(BuildingType buildingType) // Should probably scale or smth
    {
        return BuildingCosts[buildingType];
    }
}
