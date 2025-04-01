using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using System;
using System.Linq;
using Unity.Mathematics;
using WaveFunctionCollapse;

public class BuildingHandler : SerializedMonoBehaviour 
{
    public Dictionary<int, List<Building>> BuildingGroups = new Dictionary<int, List<Building>>();
    public Dictionary<int2, BuildingData> BuildingData = new Dictionary<int2, BuildingData>();

    [Title("Mesh Information")]
    [SerializeField]
    private TowerMeshData towerMeshData;
    
    [SerializeField]
    private BuildableCornerData cornerData;
    
    [Title("District")]
    [SerializeField]
    private IChunkWaveFunction districtGenerator;
    
    [Title("Data")]
    [SerializeField]
    private WallData wallData;

    private List<Building> buildingQueue = new List<Building>();
    private HashSet<Building> unSelectedBuildings = new HashSet<Building>();

    private int selectedGroupIndex = -1;
    private int groupIndexCounter;
    private bool chilling;

    public BuildingData this[Building building] => BuildingData.GetValueOrDefault(building.Index);  // I like this way too much

    #region Handling Groups

    public async UniTask AddBuilding(Building building)
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
        buildingQueue.ForEach(build => build.BuildingGroupIndex = groupIndexCounter);
        buildingQueue.Clear();

        CheckMerge(groupIndexCounter);
    }

    private void UpdateData(BuildingData buildingData, Building building)
    {
        if (building.Prototype.MeshRot.Mesh is null) // Full
        {
            buildingData.OnBuildingChanged(new BuildingCellInformation { HouseCount = 4, TowerType = TowerType.None }, building);
            return; 
        }
        
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
            //Debug.Log("Please add all meshes to the list");

            BuildingData wrongdata = new BuildingData(this, wallData.Stats, building.Index);
            //wrongdata.SetState(new BuildingCellInformation { HouseCount = 1, TowerType = TowerType.None}, building.Index, building.Prototype);

            return wrongdata;
        }

        BuildingData data = new BuildingData(this, wallData.Stats, building.Index);
        //data.SetState(cellInfo, building.Index, building.Prototype);

        return data;
    }

    private void CheckMerge(int groupToCheck)
    {
        List<Building> buildings = BuildingGroups[groupToCheck];
        int adjacenyCount = 0;

        foreach (KeyValuePair<int, List<Building>> group in BuildingGroups)
        {
            if (group.Key == groupToCheck) continue;
            
            for (int i = buildings.Count - 1; i >= 0; i--)
            {
                Building building = buildings[i];
                bool any = false;
                for (int j = 0; j < group.Value.Count && !any; j++)
                {
                    Building otherBuilding = group.Value[j];
                    if (!IsAdjacent(building, otherBuilding)) continue;
                    if (++adjacenyCount < 1) continue;

                    any = true;
                }

                if (!any) continue;
                
                Merge(groupToCheck, group.Key);
                CheckMerge(group.Key);
                return;
            }
        }
    }

    private void Merge(int groupToMerge, int targetGroup)
    {
        BuildingGroups[groupToMerge].ForEach(building => building.BuildingGroupIndex = targetGroup);
        BuildingGroups[targetGroup].AddRange(BuildingGroups[groupToMerge]);
        BuildingGroups.Remove(groupToMerge);
    }

    private bool IsAdjacent(Building building1, Building building2)
    {
        int2 indexDiff = building1.Index - building2.Index;
        if (math.abs(indexDiff.x) + math.abs(indexDiff.y) > 1)
        {
            //print("Diff: " + indexDiff + " has magnitude: " + indexDiff.magnitude);
            return false;
        }
        
        Vector2Int dir = indexDiff.y == 0 
            ? new Vector2Int(indexDiff.x, 1)
            : new Vector2Int(1, indexDiff.y);
        Vector2Int otherDir = indexDiff.y == 0 
            ? new Vector2Int(indexDiff.x, -1)
            : new Vector2Int(-1, indexDiff.y);
        return cornerData.IsCornerBuildable(building1.MeshRot, dir, out _) || cornerData.IsCornerBuildable(building1.MeshRot, otherDir, out _);
    }

    public void RemoveBuilding(Building building)
    {
        if (BuildingGroups.TryGetValue(building.BuildingGroupIndex, out var list))
        {
            list.Remove(building);
        }
    }

    public void BuildingDestroyed(int2 buildingIndex)
    {
        Building building = GetBuilding(buildingIndex);
        if (building == null)
        {
            Debug.Log("Building null at: " + buildingIndex);
            return;
        }
        
        building.OnDestroyed();
        Events.OnBuildingDestroyed?.Invoke(building);
    }

    #endregion

    #region Utility

    public Building GetBuilding(int2 buildingIndex)
    {
        foreach (List<Building> list in BuildingGroups.Values)
        {
            foreach (Building building in list)
            {
                if (math.all(building.Index == buildingIndex))
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
            built.Highlight(BuildingData[built.Index].CellInformation).Forget(ex =>
            {
                Debug.LogError($"Async function failed: {ex}");
            });;
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

        if (BuildingGroups.ContainsKey(selectedGroupIndex) && unSelectedBuildings.Count < BuildingGroups[selectedGroupIndex].Count)
        {
            return;
        }

        LowLightBuildings();
    }

    private void LowLightBuildings()
    {
        if (selectedGroupIndex == -1 || !BuildingGroups.TryGetValue(selectedGroupIndex, out List<Building> group)) return;

        foreach (var item in group)
        {
            item.Lowlight();
        }

        unSelectedBuildings.Clear();
    }

    public void DislpayLevelUp(int2 index)
    {
        GetBuilding(index).DisplayLevelUp();
    }


    #endregion
}

[System.Serializable]
public struct BuildingCellInformation : IEquatable<BuildingCellInformation>
{
    public int HouseCount;
    public bool Upgradable;
    public TowerType TowerType;
    
    public override bool Equals(object obj)
    {
        return obj is BuildingCellInformation information &&
               information.Equals(this);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(HouseCount, Upgradable, TowerType);
    }

    public bool Equals(BuildingCellInformation other)
    {
        return HouseCount == other.HouseCount 
               && Upgradable == other.Upgradable 
               && TowerType == other.TowerType;
    }
}

public enum TowerType
{
    None,
    Archer,
    Bomb,
    Church
}
