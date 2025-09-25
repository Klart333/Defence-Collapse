using System.Collections.Generic;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Buildings.District;
using Cysharp.Threading.Tasks;
using Effects.ECS;
using Gameplay.Chunk.ECS;
using Unity.Mathematics;
using Gameplay.Money;
using Unity.Entities;
using Pathfinding;
using UnityEngine;

namespace Buildings.Lumbermill
{
    public class LumbermillHandler : MonoBehaviour
    {
        [Title("References")]
        [SerializeField]
        private DistrictHandler districtHandler;
        
        [SerializeField]
        private DistrictGenerator districtGenerator;
        
        [SerializeField]
        private GroundGenerator groundGenerator;
        
        [Title("Reward")]
        [SerializeField]
        private float moneyReward = 300;

        [SerializeField]
        private int barricadeAmount = 2;

        [Title("Visual")]
        [SerializeField]
        private float treeRemovalPercentage = 0.12f;
        
        private EntityManager entityManager;
        
        private void OnEnable()
        {
            districtHandler.OnDistrictCreated += OnDistrictCreated;
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        private void OnDisable()
        {
            districtHandler.OnDistrictCreated -= OnDistrictCreated;
        }

        // ReSharper disable AccessToDisposedClosure
        private void OnDistrictCreated(DistrictData districtData)
        {
            if (districtData.State is not LumbermillState state) return;
            
            state.OnStatisticsChanged += StateOnOnStatisticsChanged;

            void StateOnOnStatisticsChanged()
            {
                HashSet<ChunkIndex> chunkIndexes = new HashSet<ChunkIndex>();
                foreach (KeyValuePair<int3, QueryChunk> kvp in districtData.DistrictChunks)
                {
                    if (districtGenerator.TryGetBuildingCell(kvp.Key, out ChunkIndex index))
                    {
                        chunkIndexes.Add(index);
                    }
                }
                bool isComplete = state.TurnsUntilComplete <= 0;

                foreach (ChunkIndex index in chunkIndexes)
                {
                    Entity entity = entityManager.CreateEntity(typeof(RemoveGroundObjectComponent), typeof(OneFrameTag));
                    Vector3 pos = ChunkWaveUtility.GetPosition(index, groundGenerator.ChunkScale, groundGenerator.ChunkWaveFunction.CellSize) - groundGenerator.ChunkWaveFunction.CellSize / 2.0f;
                    entityManager.SetComponentData(entity, new RemoveGroundObjectComponent
                    {
                        PathIndex = PathUtility.GetIndex(pos.XZ()),
                        Percentage = isComplete ? 1.0f : treeRemovalPercentage,
                    });
                }
                
                if (!isComplete) return;
                
                state.OnStatisticsChanged -= StateOnOnStatisticsChanged;
                
                districtGenerator.AddAction(async () => await districtGenerator.RemoveChunks(chunkIndexes));
                districtData.Dispose();

                foreach (ChunkIndex chunkIndex in chunkIndexes)
                {
                    GrantReward(chunkIndex);
                }
            }
        }

        private void GrantReward(ChunkIndex chunkIndex)
        {
            Vector3 position = ChunkWaveUtility.GetPosition(chunkIndex, groundGenerator.ChunkScale, groundGenerator.ChunkWaveFunction.CellSize);
            MoneyManager.Instance.AddMoneyParticles(moneyReward, position);
            groundGenerator.ChangeGroundType(position.XZ(), GroundType.Grass);
        }
    }
}