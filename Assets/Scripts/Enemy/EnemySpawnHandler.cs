using Random = Unity.Mathematics.Random;

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Buildings.District;
using Unity.Mathematics;
using Unity.Entities;
using UnityEngine;
using Pathfinding;
using Enemy.ECS;
using Gameplay;
using System;
using Sirenix.Utilities;

namespace Enemy
{
    public class EnemySpawnHandler : MonoBehaviour
    {
        private struct PortalData : IEquatable<PortalData>
        {
            public int3 ChunkIndex;
            public PathIndex PathIndex;
            public int SpawnIndex;

            public bool Equals(PortalData other)
            {
                return ChunkIndex.Equals(other.ChunkIndex);
            }

            public override bool Equals(object obj)
            {
                return obj is PortalData other && Equals(other);
            }

            public override int GetHashCode()
            {
                return ChunkIndex.GetHashCode();
            }
        }
        
        public static SpawnDataUtility SpawnDataUtility;
        
        [Title("References")]
        [SerializeField]
        private DistrictHandler districtHandler;
        
        [SerializeField]
        private GroundGenerator groundGenerator;
        
        [SerializeField]
        private SpawnDataUtility spawnDataUtility;
        
        [SerializeField]
        private BuildableCornerData groundCornerData;

        private HashSet<PortalData> spawnedPortals = new HashSet<PortalData>();
        private HashSet<PortalData> portalIndexes = new HashSet<PortalData>();
        
        private DistrictData townHallData;
        private Random random;
            
        private EntityManager entityManager;
        private Entity spawnPrefab;
        
        private int spawnIndex;

        private void Awake()
        {
            SpawnDataUtility = spawnDataUtility;
            spawnIndex = 0;
        }

        private void OnEnable()
        {
            districtHandler.OnDistrictCreated += OnDistrictBuilt;
            
            groundGenerator.OnCellCollapsed += OnCellCollapsed;
            groundGenerator.OnGenerationFinished += OnGenerationFinished;
            
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            spawnPrefab = entityManager.CreateEntity(typeof(SpawnPointComponent), typeof(Prefab));
            
            GetGameManager().Forget();
        }
        
        private void OnDisable()
        {
            groundGenerator.OnGenerationFinished -= OnGenerationFinished;
            districtHandler.OnDistrictCreated -= OnDistrictBuilt;
            groundGenerator.OnCellCollapsed -= OnCellCollapsed;
        }
        
        private async UniTaskVoid GetGameManager()
        {
            GameManager gameManager = await GameManager.Get();
            random = new Random(gameManager.Seed);
        }
        
        private void OnDistrictBuilt(DistrictData districtData)
        {
            if (districtData.TowerData.DistrictType != DistrictType.TownHall)
            {
                Debug.LogError("Should only be called with DistrictType.TownHall");
                return;
            }
            
            townHallData = districtData;
            districtHandler.OnDistrictCreated -= OnDistrictBuilt;
        }
        
        private void OnGenerationFinished()
        {
            foreach (PortalData portalData in portalIndexes)
            {
                if (!groundGenerator.LoadedFullChunks.Contains(portalData.ChunkIndex)
                    || !spawnedPortals.Add(portalData)) continue;

                CreateEntity(portalData);
            }
        }
        
        private void OnCellCollapsed(ChunkIndex chunkIndex)
        {
            PrototypeData prot = groundGenerator.ChunkWaveFunction[chunkIndex].PossiblePrototypes[0];
            for (int i = 0; i < WaveFunctionUtility.Corners.Length; i++)
            {
                int2 corner = WaveFunctionUtility.Corners[i];
                if (!groundCornerData.TryGetCornerType(prot.MeshRot, corner, out GroundType groundType)) continue;
                if (groundType != GroundType.Portal) continue;

                Vector3 pos = ChunkWaveUtility.GetPosition(chunkIndex, groundGenerator.ChunkScale, groundGenerator.ChunkWaveFunction.CellSize) 
                    + (Vector3)corner.MultiplyByAxis(groundGenerator.ChunkWaveFunction.CellSize.XZ() / 2.0f).XyZ();

                if (portalIndexes.Add(new PortalData
                    {
                        PathIndex = PathUtility.GetIndex(pos.XZ()),
                        ChunkIndex = chunkIndex.Index,
                        SpawnIndex = spawnIndex,
                    }))
                {
                    spawnIndex++;
                }
            }
        }

        private void CreateEntity(PortalData portalData)
        {
            float3 pathPosition = PathUtility.GetPos(portalData.PathIndex);
            Entity entity = entityManager.Instantiate(spawnPrefab);
            entityManager.SetComponentData(entity, new SpawnPointComponent
            {
                Position = pathPosition,
                Index = portalData.SpawnIndex,
                Random = Random.CreateFromIndex(random.NextUInt(1, 20000000))
            });
        }

        public int GetFurthestSpawnIndex()
        {
            float3 townHallPosition = townHallData.Position;
            float furthest = 0;
            int furthestSpawnIndex = -1;
            foreach (PortalData portalData in spawnedPortals)
            {
                float dist = math.distancesq(townHallPosition, PathUtility.GetPos(portalData.PathIndex));
                if (dist > furthest)
                {
                    furthest = dist;
                    furthestSpawnIndex = portalData.SpawnIndex;
                }
            }

            return furthestSpawnIndex;
        }
    }
}