using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Unity.Mathematics;
using UnityEngine;
using System;
using DataStructures.Queue.ECS;
using Pathfinding;
using Unity.Collections;

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

    [Title("Debug")]
    [SerializeField]
    private bool verbose = true;

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
        await UniTask.Yield();

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
        chilling = false;
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
        List<ChunkIndex> builtIndexes = BuildingManager.Instance.GetSurroundingMarchedIndexes(building.ChunkIndex);
        foreach (ChunkIndex chunkIndex in builtIndexes)
        {
            if (Buildings.TryGetValue(chunkIndex, out List<Building> buildings))
            {
                buildings.RemoveSwapBack(building);
            }    
        }
        
        if (!BuildingGroups.TryGetValue(building.BuildingGroupIndex, out List<Building> list)) return;
        
        list.RemoveSwapBack(building);
        if (list.Count == 0)
        {
            BuildingGroups.Remove(building.BuildingGroupIndex);
        }
    }
    
    public void BuildingTakeDamage(ChunkIndex index, float damage, PathIndex pathIndex)
    {
        if (verbose)
        {
            //Debug.Log($"Damaged {damage} at {index}");
        }
        
        List<ChunkIndex> damageIndexes = BuildingManager.Instance.GetSurroundingMarchedIndexes(index);
        damage /= damageIndexes.Count;
        bool didDamage = false;
        for (int i = 0; i < damageIndexes.Count; i++)
        {
            if (WallStates.TryGetValue(damageIndexes[i], out WallState state))
            {
                state.TakeDamage(damage);
                didDamage = true;
            }
        }

        if (!didDamage)
        {
            AttackingSystem.DamageEvent.Remove(pathIndex);
            StopAttackingSystem.KilledIndexes.Enqueue(pathIndex);
        }
    }

    public async UniTask BuildingDestroyed(ChunkIndex chunkIndex)
    {
        BuildingManager.Instance.RevertQuery();
        if (chilling)
        {
            await UniTask.WaitWhile(() => chilling);
        }
        
        WallStates.Remove(chunkIndex);
        Events.OnBuiltIndexDestroyed?.Invoke(chunkIndex);
        
        if (!Buildings.Remove(chunkIndex, out List<Building> buildings)) return;

        List<ChunkIndex> destroyedIndexes = new List<ChunkIndex>();
        for (int i = 0; i < buildings.Count; i++)
        {
            if (BuildingManager.Instance.GetSurroundingMarchedIndexes(buildings[i].ChunkIndex).Count > 0) continue;
            
            buildings[i].OnDestroyed();
            destroyedIndexes.Add(buildings[i].ChunkIndex);
        }

        if (destroyedIndexes.Count > 0)
        {
            Events.OnWallsDestroyed?.Invoke(destroyedIndexes);
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
