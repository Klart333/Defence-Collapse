using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public enum TowerType
{
    Archer
}

public class BuildingHandler : SerializedMonoBehaviour 
{
    public Dictionary<int, List<Building>> BuildingGroups = new Dictionary<int, List<Building>>();

    [Title("Mesh Information")]
    [SerializeField]
    private Dictionary<Mesh, TowerType> TowerMeshes = new Dictionary<Mesh, TowerType>();

    private List<Building> buildingQueue = new List<Building>();

    private int groupIndexCounter = 0;
    private bool chilling = false;

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
        if (!TowerMeshes.TryGetValue(building.Mesh, out TowerType value))
        {
            return;
        }

        switch (value)
        {
            case TowerType.Archer:
                building.SetState<ArcherState>();
                break;
            default:
                break;
        }
    }

    public void HighlightGroup(int groupIndex)
    {
        List<Building> buildings = BuildingGroups[groupIndex];
        foreach (Building building in buildings)
        {
            building.Highlight();
        }
    }
}
