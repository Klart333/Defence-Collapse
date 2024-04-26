using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;


public class BuildingHandler : SerializedMonoBehaviour 
{
    public Dictionary<int, List<Building>> BuildingGroups = new Dictionary<int, List<Building>>();

    [Title("Mesh Information")]
    [SerializeField]
    private TowerMeshData towerMeshData;

    private List<Building> buildingQueue = new List<Building>();
    private HashSet<Building> unSelectedBuildings = new HashSet<Building>();

    private int selectedGroupIndex = -1;
    private int groupIndexCounter = 0;
    private bool chilling = false;

    #region Handling Groups
    public async void AddBuilding(Building building)
    {
        buildingQueue.Add(building);
        UpdateBuildingState(building);
        if (chilling) return;

        chilling = true;
        await Task.Yield();
        chilling = false;

        BuildingGroups.Add(++groupIndexCounter, new List<Building>(buildingQueue));
        buildingQueue.ForEach(building => building.BuildingGroupIndex = groupIndexCounter);
        buildingQueue.Clear();

        CheckMerge(groupIndexCounter);
    }

    private void CheckMerge(int groupToCheck)
    {
        List<Building> buildings = BuildingGroups[groupToCheck];
        int adjacenyCount = 0;

        foreach (KeyValuePair<int, List<Building>> group in BuildingGroups)
        {
            if (group.Key == groupToCheck)
            {
                continue;
            }

            for (int i = 0; i < buildings.Count; i++)
            {
                Building building = buildings[i];
                for (int g = 0; g < group.Value.Count; g++)
                {
                    if (IsAdjacent(building, group.Value[g]))
                    {
                        if (++adjacenyCount < 2)
                        {
                            continue;
                        }
                        Merge(groupToCheck, group.Key);
                        CheckMerge(group.Key);
                        return;
                    }
                }
            }
            
        }
    }

    private void Merge(int groupToMerge, int targetGroup)
    {
        BuildingGroups[groupToMerge].ForEach(building => building.BuildingGroupIndex = targetGroup);
        BuildingGroups[targetGroup].AddRange(BuildingGroups[groupToMerge]);
        BuildingGroups.Remove(groupToMerge);
    }

    private bool IsAdjacent(Building building1, Building building2, float scale = 2)
    {
        float distance = Vector3.Distance(building1.transform.position, building2.transform.position);
        return distance <= scale;
    }

    public void RemoveBuilding(Building building)
    {
        if (BuildingGroups.TryGetValue(building.BuildingGroupIndex, out var list))
        {
            list.Remove(building);
        }
    }

    public void UpdateBuildingState(Building building)
    {
        if (!towerMeshData.TowerMeshes.TryGetValue(building.Mesh, out BuildingCellInformation value))
        {
            return;
        }

        switch (value.TowerType)
        {
            case TowerType.Archer:
                building.SetState<ArcherState>();
                break;
            default:
                break;
        }
    }
    #endregion

    #region Utility

    public int GetHouseCount(int groupIndex)
    {
        int total = 0;
        foreach (var item in BuildingGroups[groupIndex])
        {
            if (towerMeshData.TowerMeshes.TryGetValue(item.Mesh, out var value))
            {
                total += value.HouseCount;
            }
        }

        return total;
    }

    #endregion

    #region Visual

    public void HighlightGroup(int groupIndex)
    {
        if (groupIndex == -1) return; 

        UnSelect(); 
        selectedGroupIndex = groupIndex;

        List<Building> buildings = BuildingGroups[groupIndex];
        foreach (Building building in buildings)
        {
            building.Highlight();
        }

        print("House count: " + GetHouseCount(selectedGroupIndex));
    }

    public void LowlightGroup(Building building)
    { 
        if (selectedGroupIndex == -1) return;

        unSelectedBuildings.Add(building);

        if (unSelectedBuildings.Count < BuildingGroups[selectedGroupIndex].Count)
        {
            return;
        }

        UnSelect();
    }

    private void UnSelect()
    {
        if (selectedGroupIndex == -1) return;

        foreach (var item in BuildingGroups[selectedGroupIndex])
        {
            item.Lowlight();
        }

        unSelectedBuildings.Clear();
    }

    #endregion
}

[System.Serializable]
public struct BuildingCellInformation
{
    public int HouseCount;
    public TowerType TowerType;
}

public enum TowerType
{
    None,
    Archer
}
