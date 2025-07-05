using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace WaveFunctionCollapse
{
    public class GroundAnimator : MonoBehaviour
    {
        [SerializeField]
        private GroundGenerator groundGenerator;

        [Title("Animation Settings")]
        [SerializeField]
        private float fallDuration = 1.0f;
        
        [SerializeField]
        private Ease fallEase = Ease.OutQuad;
        
        private void OnEnable()
        {
            groundGenerator.OnCellCollapsed += OnCellCollapsed;
        }

        private void OnDisable()
        {
            groundGenerator.OnCellCollapsed -= OnCellCollapsed;
        }

        private void OnCellCollapsed(ChunkIndex index)
        {
            Transform cellTransform = groundGenerator.ChunkWaveFunction.Chunks[index.Index].SpawnedMeshes[^1].transform;
            Vector3 position = cellTransform.position + Vector3.down * 0.1f;
            cellTransform.DORewind();

            Vector3 targetScale = cellTransform.localScale;
            cellTransform.localScale = Vector3.zero;
            cellTransform.DOScale(targetScale, fallDuration/4.0f).SetEase(fallEase);
            
            cellTransform.position += Vector3.up * 5; 
            cellTransform.DOMove(position, fallDuration).SetEase(fallEase);
        }
    }
}