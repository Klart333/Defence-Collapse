using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using DG.Tweening;
using UnityEngine;
using Utility;
using System;

namespace WaveFunctionCollapse
{
    public class GroundAnimator : MonoBehaviour
    {
        public event Action OnAnimationFinished;
        
        [Title("References")]
        [SerializeField]
        private GroundGenerator groundGenerator;

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
        
        private DeletableQueue<Tuple<Transform, Vector3>> builtQueue = new DeletableQueue<Tuple<Transform, Vector3>>();
        
        private bool queueIsEmpty = true;
        private float timer;
        
        private void OnEnable()
        {
            groundGenerator.OnCellCollapsed += OnCellCollapsed;
        }

        private void OnDisable()
        {
            groundGenerator.OnCellCollapsed -= OnCellCollapsed;
        }

        private void Update()
        {
            if (timer > -0.016f)
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
                if (!builtQueue.TryDequeue(out Tuple<Transform, Vector3> index))
                {
                    if (!queueIsEmpty)
                    {  
                        DelayedComplete().Forget();
                        queueIsEmpty = true;
                    }
                    
                    return;
                }
                if (index.Item1 == null) return;

                float count = builtQueue.Count;
                timer += defaultDelayMs * Mathf.Clamp01(queueSpeedCapacity / count);
                AnimateGroundCell(index.Item1, index.Item2);
            } while (timer < 0);
        }

        private void OnCellCollapsed(ChunkIndex index)
        {
            if (groundGenerator.ChunkWaveFunction.Chunks[index.Index].SpawnedMeshes.Count == 0)
            {
                return;
            }
            
            queueIsEmpty = false;
            
            Transform cellTransform = groundGenerator.ChunkWaveFunction.Chunks[index.Index].SpawnedMeshes[^1].transform;
            Vector3 targetScale = cellTransform.transform.localScale;
            cellTransform.transform.localScale = Vector3.zero;
            
            builtQueue.Enqueue(Tuple.Create(cellTransform, targetScale));
        }

        private void AnimateGroundCell(Transform cellTransform, Vector3 targetScale)
        {
            cellTransform.transform.DOScale(targetScale, fallDuration/4.0f).SetEase(fallEase);
            
            Vector3 position = cellTransform.transform.position + Vector3.down * 0.1f;
            cellTransform.transform.position += Vector3.up * height;
            cellTransform.transform.DOMove(position, fallDuration).SetEase(fallEase);
        }

        private async UniTaskVoid DelayedComplete()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(fallDuration));

            if (builtQueue.Count > 0)
            {
                return;
            }
            OnAnimationFinished?.Invoke();
        }
    }
}