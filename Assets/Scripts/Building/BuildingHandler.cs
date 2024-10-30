using System.Collections.Generic;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using System;

public class BuildingHandler : SerializedMonoBehaviour 
{
    public Dictionary<int, List<Building>> BuildingGroups = new Dictionary<int, List<Building>>();
    public Dictionary<Vector3Int, BuildingData> BuildingData = new Dictionary<Vector3Int, BuildingData>();

    [Title("Mesh Information")]
    [SerializeField]
    private TowerMeshData towerMeshData;

    private List<Building> buildingQueue = new List<Building>();
    private HashSet<Building> unSelectedBuildings = new HashSet<Building>();

    private int selectedGroupIndex = -1;
    private int groupIndexCounter = 0;
    private bool chilling = false;

    public BuildingData this[Building building]
    {
        get 
        { 
            if (BuildingData.TryGetValue(building.Index, out BuildingData data))
            {
                return data;
            }

            return null; 
        }
    } // I like this way too much

    #region Handling Groups

    public async void AddBuilding(Building building)
    {
        buildingQueue.Add(building);
        if (chilling) return;

        chilling = true;
        await Task.Yield();
        chilling = false;

        for (int i = 0; i < buildingQueue.Count; i++)
        {
            if (!BuildingData.ContainsKey(buildingQueue[i].Index))
            {
                BuildingData.Add(buildingQueue[i].Index, CreateData(buildingQueue[i]));
                continue;
            }

            UpdateData(BuildingData[buildingQueue[i].Index], buildingQueue[i]);
        }

        BuildingGroups.Add(++groupIndexCounter, new List<Building>(buildingQueue));
        buildingQueue.ForEach(building => building.BuildingGroupIndex = groupIndexCounter);
        buildingQueue.Clear();

        CheckMerge(groupIndexCounter);
    }

    private void UpdateData(BuildingData buildingData, Building building)
    {
        if (!towerMeshData.TowerMeshes.TryGetValue(building.Prototype.MeshRot.Mesh, out BuildingCellInformation cellInfo))
        {
            buildingData.OnBuildingChanged(new BuildingCellInformation { HouseCount = 1, TowerType = TowerType.None }, building);
        }

        buildingData.OnBuildingChanged(cellInfo, building);
    }

    private BuildingData CreateData(Building building)
    {
        if (!towerMeshData.TowerMeshes.TryGetValue(building.Prototype.MeshRot.Mesh, out BuildingCellInformation cellInfo))
        {
            Debug.Log("Please add all meshes to the list");

            BuildingData wrongdata = new BuildingData(this);
            wrongdata.SetState(new BuildingCellInformation { HouseCount = 1, TowerType = TowerType.None}, building.Index, building.Prototype);

            return wrongdata;
        }

        BuildingData data = new BuildingData(this);
        data.SetState(cellInfo, building.Index, building.Prototype);

        return data;
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

    public void BuildingDestroyed(Vector3Int buildingIndex)
    {
        Building building = GetBuilding(buildingIndex);

        building.DisplayDeath();
        Events.OnBuildingDestroyed?.Invoke(building);
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

    public Building GetBuilding(Vector3Int buildingIndex)
    {
        foreach (List<Building> list in BuildingGroups.Values)
        {
            foreach (Building building in list)
            {
                if (building.Index == buildingIndex)
                {
                    return building;
                }
            }
        }

        return null;
    }


    #endregion

    #region Visual

    public void HighlightGroup(Building building)
    {
        if (building.BuildingGroupIndex == -1) return;

        if (selectedGroupIndex != building.BuildingGroupIndex)
        {
            LowLightBuildings();
        }

        selectedGroupIndex = building.BuildingGroupIndex;

        List<Building> buildings = BuildingGroups[building.BuildingGroupIndex];
        foreach (Building built in buildings)
        {
            built.Highlight(BuildingData[built.Index].CellInformation);
        }

        building.OnSelected(BuildingData[building.Index].CellInformation);

        if (unSelectedBuildings.Contains(building))
        {
            unSelectedBuildings.Remove(building);
        }
    }

    public void LowlightGroup(Building building)
    { 
        if (selectedGroupIndex == -1) return;

        unSelectedBuildings.Add(building);
        building.OnDeselected();

        if (unSelectedBuildings.Count < BuildingGroups[selectedGroupIndex].Count)
        {
            return;
        }

        LowLightBuildings();
    }

    private void LowLightBuildings()
    {
        if (selectedGroupIndex == -1) return;

        foreach (var item in BuildingGroups[selectedGroupIndex])
        {
            item.Lowlight();
        }

        unSelectedBuildings.Clear();
    }

    public void DislpayLevelUp(Vector3Int index)
    {
        GetBuilding(index).DisplayLevelUp();
    }


    #endregion
}

[System.Serializable]
public struct BuildingCellInformation
{
    public int HouseCount;
    public bool Upgradable;
    public TowerType TowerType;

    public override bool Equals(object obj)
    {
        return obj is BuildingCellInformation information &&
               HouseCount == information.HouseCount &&
               Upgradable == information.Upgradable &&
               TowerType == information.TowerType;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(HouseCount, Upgradable, TowerType);
    }
}

public enum TowerType
{
    None,
    Archer,
    Bomb,
    Church
}
