using Random = Unity.Mathematics.Random;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Unity.Mathematics;
using Gameplay.Money;
using Gameplay.Event;
using UnityEngine;
using Buildings;
using Utility;
using System;

namespace Chunks
{
    [Serializable]
    public struct PooledArray
    {
        public PooledMonoBehaviour[] Array;
    }
    
    public class TreeGrower : PooledMonoBehaviour
    {
        public event Action<TreeGrower> OnPlaced;
        
        [Title("Tree")]
        [SerializeField]
        private PooledArray[] trees;

        [SerializeField]
        private GroundType objectGroundType;

        [SerializeField]
        private Vector2 raycastArea;

        [SerializeField]
        private float treeRadius = 0.2f;

        [SerializeField]
        private int raycastCount = 100;

        [SerializeField]
        private float totalTime = 2.0f;
        
        [SerializeField]
        private AtlasAnalyzer atlasAnalyzer;
        
        [Title("Remove")]
        [SerializeField]
        private bool shouldRemoveWhenPlaced = true;

        [SerializeField, ShowIf(nameof(shouldRemoveWhenPlaced))]
        private float goldPerTree = 5;

        [SerializeField, ShowIf(nameof(shouldRemoveWhenPlaced))]
        private float distanceMultiplier = 3;

        private readonly List<PooledMonoBehaviour> spawnedTrees = new List<PooledMonoBehaviour>();
        
        private float treeRadiusSq;
        
        public ChunkIndex ChunkIndex { get; set; }
        public bool HasGrown { get; set; }
        public Cell Cell { get; set; }
        
        public bool ShouldRemoveWhenPlaced => shouldRemoveWhenPlaced;

        private void OnEnable()
        {
            if (shouldRemoveWhenPlaced)
            {
                Events.OnBuildingBuilt += OnBuildingBuilt;   
            }
            
            treeRadiusSq = treeRadius * treeRadius;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            
            spawnedTrees.Clear();
            HasGrown = false;
            
            Events.OnBuildingBuilt -= OnBuildingBuilt;   
        }

        [Button]
        public async UniTaskVoid GrowTrees(int groupIndex, Random random)
        {
            Vector3 offset = raycastArea.ToXyZ(1).MultiplyByAxis(transform.localScale) / 2.0f;
            Vector3 min = transform.position - offset;
            Vector3 max = transform.position + offset;
            List<float2> positions = new List<float2>();
            TimeSpan delay = TimeSpan.FromSeconds(totalTime / raycastCount);
            
            List<Color> colors = atlasAnalyzer.GetAtlasColorsBuildable(out List<GroundType> groundTypes);
            for (int i = 0; i < raycastCount; i++)
            {
                Vector3 pos = new Vector3(random.NextFloat(min.x, max.x), 2, random.NextFloat(min.z, max.z));
                if (!CheckCollisionWithTrees(new float2(pos.x, pos.z))) continue;

                Ray ray = new Ray(pos, Vector3.down);
                if (!Physics.Raycast(ray, out RaycastHit hit)) continue;
                
                Color color = atlasAnalyzer.Atlas.GetPixel((int)(hit.textureCoord.x * atlasAnalyzer.Atlas.width), (int)(hit.textureCoord.y * atlasAnalyzer.Atlas.height));
                int index = colors.IndexOf(color);
                if ((groundTypes[index] & objectGroundType) != objectGroundType) continue;
                
                positions.Add(pos.XZ());
                SpawnTree(hit.point, groupIndex, random);
                await UniTask.Delay(delay);
            }

            return;
            
            bool CheckCollisionWithTrees(float2 pos)
            {
                for (int j = 0; j < positions.Count; j++)
                {
                    if (math.distancesq(positions[j], pos) < treeRadiusSq)
                    {
                        return false;
                    }
                }   

                return true;
            }
        }
        
        private void SpawnTree(Vector3 pos, int groupIndex, Random random)
        {
            PooledMonoBehaviour treePrefab = trees[groupIndex].Array[random.NextInt(0, trees[groupIndex].Array.Length)];
            Quaternion rot = Quaternion.AngleAxis(random.NextFloat() * 360, Vector3.up);
            PooledMonoBehaviour spawned = treePrefab.GetAtPosAndRot<PooledMonoBehaviour>(pos, rot);
            spawnedTrees.Add(spawned);
        }

        public void ClearTrees()
        {
            for (int i = 0; i < spawnedTrees.Count; i++)
            {
                spawnedTrees[i].gameObject.SetActive(false);
            }
            
            spawnedTrees.Clear();
        }
        
        private void OnBuildingBuilt(ICollection<IBuildable> buildables)
        {
            foreach (IBuildable buildable in buildables)
            {
                if (buildable is not Building || !ChunkIndex.Equals(buildable.ChunkIndex)) continue;
                
                Placed();
                break;
            }

            void Placed()
            {
                int distance = Mathf.Abs(ChunkIndex.Index.x) + Mathf.Abs(ChunkIndex.Index.z);
                float gold = goldPerTree + goldPerTree * distanceMultiplier * distance;
                gold *= MoneyManager.Instance.MoneyMultiplier.Value;

                foreach (PooledMonoBehaviour tree in spawnedTrees)
                {
                    MoneyManager.Instance.AddMoneyParticles(gold, tree.transform.position);
                }

                ClearTrees();
                OnPlaced?.Invoke(this);
            }
        }

        #region Debug

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireCube(transform.position + Vector3.up, raycastArea.ToXyZ(1).MultiplyByAxis(transform.localScale));
        }

        #endregion
    }
}
