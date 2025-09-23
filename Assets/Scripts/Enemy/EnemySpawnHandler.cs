using Random = Unity.Mathematics.Random;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Unity.Mathematics;
using Unity.Entities;
using UnityEngine;
using Pathfinding;
using Enemy.ECS;
using Gameplay;

namespace Enemy
{
    public class EnemySpawnHandler : MonoBehaviour
    {
        public static SpawnDataUtility SpawnDataUtility;
        
        [Title("References")]
        [SerializeField]
        private GroundGenerator groundGenerator;
        
        [SerializeField]
        private SpawnDataUtility spawnDataUtility;
        
        [SerializeField]
        private BuildableCornerData groundCornerData;

        private HashSet<(PathIndex, int3)> portalIndexes = new HashSet<(PathIndex, int3)>();
        
        private EntityManager entityManager;
        private Entity spawnPrefab;
        private Random random;
        
        private int spawnIndex;

        private void Awake()
        {
            SpawnDataUtility = spawnDataUtility;
        }

        private void OnEnable()
        {
            groundGenerator.OnCellCollapsed += OnCellCollapsed;
            groundGenerator.OnGenerationFinished += OnGenerationFinished;
            
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            spawnPrefab = entityManager.CreateEntity(typeof(SpawnPointComponent), typeof(Prefab));
            
            GetGameManager().Forget();
        }
        
        private void OnDisable()
        {
            groundGenerator.OnCellCollapsed -= OnCellCollapsed;
            groundGenerator.OnGenerationFinished -= OnGenerationFinished;
        }
        
        private async UniTaskVoid GetGameManager()
        {
            GameManager gameManager = await GameManager.Get();
            random = new Random(gameManager.Seed);
        }
        
        private void OnGenerationFinished()
        {
            foreach ((PathIndex, int3) pathIndex in portalIndexes)
            {
                if (!groundGenerator.LoadedFullChunks.Contains(pathIndex.Item2)) continue;
                
                CreateEntity(pathIndex.Item1);
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
                portalIndexes.Add((PathUtility.GetIndex(pos.XZ()), chunkIndex.Index));
            }
        }

        private void CreateEntity(PathIndex pathIndex)
        {
            float3 pathPosition = PathUtility.GetPos(pathIndex);
            Entity entity = entityManager.Instantiate(spawnPrefab);
            entityManager.SetComponentData(entity, new SpawnPointComponent
            {
                Position = pathPosition,
                Index = spawnIndex++,
                Random = Random.CreateFromIndex(random.NextUInt(1, 20000000))
            });
        }
    }
}