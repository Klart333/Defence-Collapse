using System.Collections.Generic;
using Buildings;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "Buildable Cost Data", menuName = "Building/Cost Data"), InlineEditor]
public class BuildableCostData : SerializedScriptableObject
{
    [Title("Costs")]
    [SerializeField]
    public Dictionary<BuildingType, int> BuildingCosts;

    public int GetCost(BuildingType buildingType)
    {
        return BuildingCosts[buildingType];
    }
}
