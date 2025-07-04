using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Sirenix.Utilities;
using Unity.Mathematics;
using UnityEngine;
using Pathfinding;
using System.Linq;
using Enemy.ECS;
using Gameplay;
using System;
using UI;

namespace Buildings
{
    public class BuildingHandler : SerializedMonoBehaviour
    {
        public event Action<WallState> OnWallStateCreated;
        
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

        [Title("References")]
        [SerializeField]
        private DistrictGenerator districtGenerator;

        [Title("Debug")]
        [SerializeField]
        private bool verbose = true;

        public readonly Dictionary<ChunkIndex, HashSet<Building>> Buildings = new Dictionary<ChunkIndex, HashSet<Building>>();
        public readonly Dictionary<int, HashSet<Building>> BuildingGroups = new Dictionary<int, HashSet<Building>>();
        public readonly Dictionary<ChunkIndex, WallState> WallStates = new Dictionary<ChunkIndex, WallState>();

        private HashSet<ChunkIndex> wallStatesWithHealth = new HashSet<ChunkIndex>();
        private HashSet<Building> unSelectedBuildings = new HashSet<Building>();

        private BuildingManager buildingManager;
        private IGameSpeed gameSpeed;

        private bool inWave;
        private int selectedGroupIndex = -1;
        private int groupIndexCounter;

        private void OnEnable()
        {
            Events.OnWaveStarted += OnWaveStarted;
            Events.OnWaveEnded += OnWaveEnded;
            
            GetBuildingManager().Forget();
            GetGameSpeed().Forget();
        }

        private void OnDisable()
        {
            Events.OnWaveStarted -= OnWaveStarted;
            Events.OnWaveEnded -= OnWaveEnded;
        }

        private void OnWaveStarted()
        {
            inWave = true;
        }

        private void OnWaveEnded()
        {
            inWave = false;
        }

        private async UniTaskVoid GetBuildingManager()
        {
            buildingManager = await BuildingManager.Get();
        }

        private async UniTaskVoid GetGameSpeed()
        {
            gameSpeed = await GameSpeedManager.Get();
        }
        
        private void Update()
        {
            if (!inWave)
            {
                return;
            }
            
            foreach (WallState wallState in WallStates.Values)
            {
                wallState.Update(Time.deltaTime * gameSpeed.Value);
            }
        }

        #region Handling Groups

        public void AddBuilding(Building building)
        {
            List<ChunkIndex> damageIndexes = buildingManager.GetSurroundingMarchedIndexes(building.ChunkIndex);

            for (int i = 0; i < damageIndexes.Count; i++)
            {
                ChunkIndex damageIndex = damageIndexes[i];

                if (!WallStates.ContainsKey(damageIndex))
                {
                    WallStates.Add(damageIndex, CreateData(damageIndex));
                }

                if (Buildings.TryGetValue(damageIndex, out HashSet<Building> buildings)) buildings.Add(building);
                else Buildings.Add(damageIndex, new HashSet<Building>(4) { building });
            }

            BuildingGroups.Add(++groupIndexCounter, new HashSet<Building> { building });
            building.BuildingGroupIndex = groupIndexCounter;
            building.PathTarget.Importance = 250;
            CheckMerge(groupIndexCounter);
        }

        private WallState CreateData(ChunkIndex chunkIndex)
        {
            Stats stats = new Stats(wallData.Stats);
            stats.MaxHealth.BaseValue *= GameData.WallHealthMultiplier.Value;
            stats.Healing.BaseValue += GameData.WallHealing.Value;
            WallState data = new WallState(this, stats, chunkIndex)
            {
                Position = buildingManager.GetPos(chunkIndex)
            };
            OnWallStateCreated?.Invoke(data);
            
            return data;
        }

        private void CheckMerge(int groupToCheck)
        {
            HashSet<Building> buildings = BuildingGroups[groupToCheck];

            foreach (KeyValuePair<int, HashSet<Building>> group in BuildingGroups)
            {
                if (group.Key == groupToCheck) continue;

                foreach (Building building in buildings)
                {
                    foreach (Building otherBuilding in group.Value)
                    {
                        if (!IsAdjacent(building, otherBuilding)) continue;

                        Merge(groupToCheck, group.Key);
                        CheckMerge(group.Key);
                        return;
                    }
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
                building.PathTarget.Importance = (byte)Mathf.Max(254 - count * 5, 10);
            }

            BuildingGroups.Remove(groupToMerge);
        }

        private bool IsAdjacent(Building building1, Building building2)
        {
            if (!ChunkWaveUtility.AreIndexesAdjacent(building1.ChunkIndex, building2.ChunkIndex, buildingManager.ChunkSize.x, out int3 indexDiff))
            {
                return false;
            }

            int2 dir = indexDiff.z == 0
                ? new int2(indexDiff.x, 1)
                : new int2(1, indexDiff.z);
            int2 otherDir = indexDiff.z == 0
                ? new int2(indexDiff.x, -1)
                : new int2(-1, indexDiff.z);
            bool isCornerBuildable = cornerData.IsCornerBuildable(building1.MeshRot, dir);
            bool otherCornerBuildable = cornerData.IsCornerBuildable(building1.MeshRot, otherDir);
            //Debug.Log($"{building1.ChunkIndex}\n{building2.ChunkIndex}\n{BuildableCornerData.VectorToCorner(dir.x, dir.y)}: {isCornerBuildable}\n{BuildableCornerData.VectorToCorner(otherDir.x, otherDir.y)}: {otherCornerBuildable}");
            return isCornerBuildable || otherCornerBuildable;
        }

        public void RemoveBuilding(Building building)
        {
            if (!BuildingGroups.TryGetValue(building.BuildingGroupIndex, out HashSet<Building> builds)) return;

            List<ChunkIndex> builtIndexes = buildingManager.GetSurroundingMarchedIndexes(building.ChunkIndex);
            foreach (ChunkIndex chunkIndex in builtIndexes)
            {
                if (Buildings.TryGetValue(chunkIndex, out HashSet<Building> buildings))
                {
                    buildings.Remove(building);
                    
                    if (buildings.Count == 0)
                    {
                        WallStates.Remove(chunkIndex);
                    }
                }
            }

            builds.Remove(building);
            if (builds.Count == 0)
            {
                BuildingGroups.Remove(building.BuildingGroupIndex);
            }
            else
            {
                int count = builds.Count;
                foreach (Building groupBuilding in builds)
                {
                    groupBuilding.PathTarget.Importance = (byte)Mathf.Max(254 - count * 5, 10);
                }
            }
        }

        public void BuildingTakeDamage(ChunkIndex index, float damage, PathIndex pathIndex)
        {
            if (verbose)
            {
                //Debug.Log($"Damaged {damage} at {index}");
            }

            List<ChunkIndex> damageIndexes = buildingManager.GetSurroundingMarchedIndexes(index);
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
                wallHealth.OnReturnToPool += OnReturnToPool;

                void OnReturnToPool(PooledMonoBehaviour obj)
                {
                    wallHealth.OnReturnToPool -= OnReturnToPool;
                    wallStatesWithHealth.Remove(damageIndex);
                }
            }
        }

        public void BuildingDestroyed(ChunkIndex chunkIndex)
        {
            buildingManager.RevertQuery();
            districtGenerator.RevertQuery();
            
            if (!Buildings.Remove(chunkIndex, out HashSet<Building> buildings))
            {
                Debug.LogError("Could not find building");
                WallStates.Remove(chunkIndex);
                Events.OnBuiltIndexDestroyed?.Invoke(chunkIndex);
                return;
            }

            List<ChunkIndex> destroyedIndexes = new List<ChunkIndex>();
            foreach (Building building in buildings)
            {
                if (buildingManager.GetSurroundingMarchedIndexes(building.ChunkIndex).Any(x => !x.Equals(chunkIndex)))
                {
                    continue;
                }
                
                destroyedIndexes.Add(building.ChunkIndex);
            }

            if (destroyedIndexes.Count > 0)
            {
                Events.OnWallsDestroyed?.Invoke(destroyedIndexes);
            }

            WallStates.Remove(chunkIndex);
            Events.OnBuiltIndexDestroyed?.Invoke(chunkIndex);
        }

        #endregion

        #region Visual

        public void HighlightGroup(Building building)
        {
            if (building.BuildingGroupIndex == -1)
            {
                Debug.LogError("Building Group Index was not found");
                return;
            }

            if (selectedGroupIndex != building.BuildingGroupIndex)
            {
                LowLightBuildings();
            }

            selectedGroupIndex = building.BuildingGroupIndex;

            HashSet<Building> buildings = BuildingGroups[building.BuildingGroupIndex];
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
            if (selectedGroupIndex == -1 || !BuildingGroups.TryGetValue(selectedGroupIndex, out HashSet<Building> group)) return;

            foreach (Building item in group)
            {
                item.Lowlight();
            }

            unSelectedBuildings.Clear();
        }

        #endregion
    }
}
