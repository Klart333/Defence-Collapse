using Sirenix.OdinInspector;
using DG.Tweening;
using UnityEngine;
using Utility;
using System;

namespace WaveFunctionCollapse
{
    public class DistrictAnimator : MonoBehaviour
    {
        [Title("References")]
        [SerializeField]
        private DistrictGenerator districtGenerator;

        [Title("Queue Settings")]
        [SerializeField]
        private float defaultDelayMs = 0.1f;

        [SerializeField]
        private int queueSpeedCapacity = 20;
        
        [Title("Animation Settings")]
        [SerializeField]
        private float height = 1.5f;
        
        [SerializeField]
        private float fallDuration = 1.0f;
        
        [SerializeField]
        private Ease fallEase = Ease.OutQuad;
        
        private DeletableQueue<Tuple<ChunkIndex, Vector3>> builtQueue = new DeletableQueue<Tuple<ChunkIndex, Vector3>>();
        
        private float timer;
        
        private void OnEnable()
        {
            districtGenerator.OnCellCollapsed += OnCellCollapsed;
        }

        private void OnDisable()
        {
            districtGenerator.OnCellCollapsed -= OnCellCollapsed;
        }

        private void Update()
        {
            if (timer > 0)
            {
                timer -= Time.deltaTime;
            }
            
            if (timer > 0) return;
            
            HandleQueue();
        }

        private void HandleQueue()
        {
            do
            {
                if (!builtQueue.TryDequeue(out Tuple<ChunkIndex, Vector3> index)) return;
                if (!districtGenerator.SpawnedMeshes.TryGetValue(index.Item1, out IBuildable buildable)) return;
                if (buildable is not PooledMonoBehaviour cellTransform) return;

                float count = builtQueue.Count;
                timer += defaultDelayMs * Mathf.Clamp01(queueSpeedCapacity / count);
                AnimateDistrictCell(cellTransform, index.Item2);
            } while (timer < 0);
        }

        private void OnCellCollapsed(ChunkIndex index)
        {
            if (districtGenerator.SpawnedMeshes[index] is not PooledMonoBehaviour cellTransform)
            {
                return;
            }
            
            Vector3 targetScale = cellTransform.transform.localScale;
            cellTransform.transform.localScale = Vector3.zero;
            
            var handle = builtQueue.Enqueue(Tuple.Create(index, targetScale));
            cellTransform.OnReturnToPool += CellTransformOnReturnToPool;

            void CellTransformOnReturnToPool(PooledMonoBehaviour obj)
            {
                cellTransform.transform.DOKill();
                cellTransform.OnReturnToPool -= CellTransformOnReturnToPool;
                builtQueue.Delete(handle);
            }
        }

        private void AnimateDistrictCell(PooledMonoBehaviour cellTransform, Vector3 targetScale)
        {
            cellTransform.transform.DOScale(targetScale, fallDuration/4.0f).SetEase(fallEase);
            
            Vector3 position = cellTransform.transform.position;
            cellTransform.transform.position += Vector3.up * height; 
            cellTransform.transform.DOMove(position, fallDuration).SetEase(fallEase).onComplete += () =>
            {
                cellTransform.OnReturnToPool -= CellTransformOnOnReturnToPool;
            };
            
            cellTransform.OnReturnToPool += CellTransformOnOnReturnToPool;

            void CellTransformOnOnReturnToPool(PooledMonoBehaviour obj)
            {
                cellTransform.OnReturnToPool -= CellTransformOnOnReturnToPool;
                cellTransform.transform.DOKill();
            }
        }
    }
}