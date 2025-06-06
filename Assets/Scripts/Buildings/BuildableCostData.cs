using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Buildings;

[CreateAssetMenu(fileName = "Buildable Cost Data", menuName = "Building/Cost Data"), InlineEditor]
public class BuildableCostData : SerializedScriptableObject
{
    [Title("Costs")]
    [SerializeField]
    public Dictionary<BuildingType, int> BuildingCosts;

    public int GetCost(BuildingType buildingType) // Should probably scale or smth
    {
        return BuildingCosts[buildingType];
    }
}
