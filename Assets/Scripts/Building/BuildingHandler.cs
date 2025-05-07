using System.Collections.Generic;
using DataStructures.Queue.ECS;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Pathfinding;
using UI;

public class BuildingHandler : SerializedMonoBehaviour 
{
    [Title("Mesh Information")]
    [SerializeField]
    private BuildableCornerData cornerData;
    
    [SerializeField]
    private ProtoypeMeshes protoypeMeshes;
    
    [Title("Data")]
    [SerializeField]
    private WallData wallData;
    
    [Title("Health")]
    [SerializeField]
    private UIWallHealth wallHealthPrefab;

    [SerializeField]
    private Canvas canvasParent;

    [Title("Debug")]
    [SerializeField]
    private bool verbose = true;

    public readonly Dictionary<ChunkIndex, HashSet<Building>> Buildings = new Dictionary<ChunkIndex, HashSet<Building>>();
    public readonly Dictionary<int, List<Building>> BuildingGroups = new Dictionary<int, List<Building>>();
    public readonly Dictionary<ChunkIndex, WallState> WallStates = new Dictionary<ChunkIndex, WallState>();

    private HashSet<ChunkIndex> wallStatesWithHealth = new HashSet<ChunkIndex>();
    private HashSet<Building> unSelectedBuildings = new HashSet<Building>();
    private List<Building> buildingQueue = new List<Building>();

    private int selectedGroupIndex = -1;
    private int groupIndexCounter;
    private bool chilling;
    
    #region Handling Groups

    public async UniTaskVoid AddBuilding(Building building)
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

                if (Buildings.TryGetValue(damageIndex, out HashSet<Building> buildings))
                {
                    buildings.Add(buildingQueue[i]);
                }
                else
                {
                    Buildings.Add(damageIndex, new HashSet<Building>(4) { buildingQueue[i] });
                }
            }
        }

        BuildingGroups.Add(++groupIndexCounter, new List<Building>(buildingQueue));
        int count = buildingQueue.Count;
        foreach (var build in buildingQueue)
        {
            build.BuildingGroupIndex = groupIndexCounter;
            build.PathTarget.Importance = (byte)Mathf.Max(255 - count * 5, 1);
        }
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
        BuildingGroups[targetGroup].AddRange(BuildingGroups[groupToMerge]);
        
        int count = BuildingGroups[targetGroup].Count;
        foreach (Building building in BuildingGroups[targetGroup])
        {
            building.BuildingGroupIndex = targetGroup;
            building.PathTarget.Importance = (byte)Mathf.Max(255 - count * 5, 1);
        }
        BuildingGroups.Remove(groupToMerge);
    }

    private bool IsAdjacent(Building building1, Building building2)
    {
        int2 indexDiff = building1.ChunkIndex.CellIndex.xz - building2.ChunkIndex.CellIndex.xz;
        if (math.abs(indexDiff.x) + math.abs(indexDiff.y) > 1) return false;
        
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
            if (Buildings.TryGetValue(chunkIndex, out HashSet<Building> buildings))
            {
                buildings.Remove(building);
            }    
        }
        
        if (!BuildingGroups.TryGetValue(building.BuildingGroupIndex, out List<Building> builds)) return;
        
        builds.RemoveSwapBack(building);
        if (builds.Count == 0)
        {
            BuildingGroups.Remove(building.BuildingGroupIndex);
        }
        else
        {
            int count = builds.Count;
            foreach (Building groupBuilding in builds)  
            {
                groupBuilding.PathTarget.Importance = (byte)Mathf.Max(255 - count * 5, 1);
            }
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
            ChunkIndex damageIndex = damageIndexes[i];
            if (!WallStates.TryGetValue(damageIndex, out WallState state)) continue;
            
            float startingHealth = state.Health.CurrentHealth;
            state.TakeDamage(damage);
            didDamage = true;

            DisplayHealth(state, damageIndex, startingHealth);
        }

        if (!didDamage)
        {
            AttackingSystem.DamageEvent.Remove(pathIndex);
            StopAttackingSystem.KilledIndexes.Enqueue(pathIndex);
        }

        void DisplayHealth(WallState state, ChunkIndex damageIndex, float startingHealth)
        {
            if (!state.Health.Alive || !wallStatesWithHealth.Add(damageIndex)) return;
            
            UIWallHealth wallHealth = wallHealthPrefab.Get<UIWallHealth>();
            wallHealth.transform.SetParent(canvasParent.transform, false);
            wallHealth.Setup(state, startingHealth, canvasParent);
            wallHealth.TweenFill();
            wallHealth.OnReturnToPool += WallHealthOnOnReturnToPool;

            void WallHealthOnOnReturnToPool(PooledMonoBehaviour obj)
            {
                wallHealth.OnReturnToPool -= WallHealthOnOnReturnToPool;
                wallStatesWithHealth.Remove(damageIndex); 
            }
        }
    }

    public async UniTaskVoid BuildingDestroyed(ChunkIndex chunkIndex)
    {
        BuildingManager.Instance.RevertQuery();
        if (chilling)
        {
            await UniTask.WaitWhile(() => chilling);
        }
        
        WallStates.Remove(chunkIndex);
        Events.OnBuiltIndexDestroyed?.Invoke(chunkIndex);
        
        if (!Buildings.Remove(chunkIndex, out HashSet<Building> buildings)) return;

        List<ChunkIndex> destroyedIndexes = new List<ChunkIndex>();
        foreach (Building building in buildings)
        {
            if (BuildingManager.Instance.GetSurroundingMarchedIndexes(building.ChunkIndex).Count > 0) continue;
            
            building.OnDestroyed();
            destroyedIndexes.Add(building.ChunkIndex);
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
            built.Highlight().Forget();
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
