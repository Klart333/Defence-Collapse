using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Unity.Mathematics;
using UnityEngine;
using System;

public class BuildingHandler : SerializedMonoBehaviour 
{
    [Title("Mesh Information")]
    [SerializeField]
    private BuildableCornerData cornerData;
    
    [SerializeField]
    private ProtoypeMeshes protoypeMeshes;
    
    [Title("District")]
    [SerializeField]
    private IChunkWaveFunction<Chunk> districtGenerator;
    
    [Title("Data")]
    [SerializeField]
    private WallData wallData;

    public readonly Dictionary<ChunkIndex, List<Building>> Buildings = new Dictionary<ChunkIndex, List<Building>>();
    public readonly Dictionary<int, List<Building>> BuildingGroups = new Dictionary<int, List<Building>>();
    public readonly Dictionary<ChunkIndex, WallState> WallStates = new Dictionary<ChunkIndex, WallState>();

    private List<Building> buildingQueue = new List<Building>();
    private HashSet<Building> unSelectedBuildings = new HashSet<Building>();

    private int selectedGroupIndex = -1;
    private int groupIndexCounter;
    private bool chilling;
    
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
            List<ChunkIndex> damageIndexes = BuildingManager.Instance.GetSurroundingMarchedIndexes(buildingQueue[i].ChunkIndex);

            for (int j = 0; j < damageIndexes.Count; j++)
            {
                ChunkIndex damageIndex = damageIndexes[j];
                if (!WallStates.ContainsKey(damageIndex))
                {
                    WallStates.Add(damageIndex, CreateData(damageIndex));
                }

                if (Buildings.TryGetValue(damageIndex, out List<Building> buildings))
                {
                    buildings.Add(buildingQueue[i]);
                }
                else
                {
                    Buildings.Add(damageIndex, new List<Building>(4) { buildingQueue[i] });
                }
            }
        }

        BuildingGroups.Add(++groupIndexCounter, new List<Building>(buildingQueue));
        buildingQueue.ForEach(build => build.BuildingGroupIndex = groupIndexCounter);
        buildingQueue.Clear();

        CheckMerge(groupIndexCounter);
    }

    private WallState CreateData(ChunkIndex chunkIndex)
    {
        WallState data = new WallState(this, wallData.Stats, chunkIndex);

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
        int2 indexDiff = building1.ChunkIndex.CellIndex.xz - building2.ChunkIndex.CellIndex.xz;
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
        if (BuildingGroups.TryGetValue(building.BuildingGroupIndex, out List<Building> list))
        {
            list.Remove(building);
        }
    }
    
    public void BuildingTakeDamage(ChunkIndex index, float damage)
    {
        List<ChunkIndex> damageIndexes = BuildingManager.Instance.GetSurroundingMarchedIndexes(index);
        damage /= damageIndexes.Count;
        for (int i = 0; i < damageIndexes.Count; i++)
        {
            WallStates[damageIndexes[i]].TakeDamage(damage);
        }
    }

    public void BuildingDestroyed(ChunkIndex chunkIndex)
    {
        WallStates.Remove(chunkIndex);
        
        BuildingManager.Instance.RemoveBuiltIndex(chunkIndex);

        if (!Buildings.TryGetValue(chunkIndex, out List<Building> buildings)) return;
        
        
        List<ChunkIndex> destroyedIndexes = new List<ChunkIndex>();
        for (int i = 0; i < buildings.Count; i++)
        {
            if (BuildingManager.Instance.GetSurroundingMarchedIndexes(buildings[i].ChunkIndex).Count > 0) continue;
            
            buildings[i].OnDestroyed();
            destroyedIndexes.Add(buildings[i].ChunkIndex);
            Events.OnWallDestroyed?.Invoke(buildings[i].ChunkIndex);
        }

        if (destroyedIndexes.Count > 0)
        {
            BuildingManager.Instance.OnIndexesDestroyed(destroyedIndexes);
        }
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
            built.Highlight().Forget(Debug.LogError);
        }

        building.OnSelected();

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

    #endregion
}
