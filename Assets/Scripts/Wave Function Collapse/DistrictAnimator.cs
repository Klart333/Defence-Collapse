using Sirenix.OdinInspector;
using DG.Tweening;
using UnityEngine;

namespace WaveFunctionCollapse
{
    public class DistrictAnimator : MonoBehaviour
    {
        [SerializeField]
        private DistrictGenerator districtGenerator;

        [Title("Animation Settings")]
        [SerializeField]
        private float height = 1.5f;
        
        [SerializeField]
        private float fallDuration = 1.0f;
        
        [SerializeField]
        private Ease fallEase = Ease.OutQuad;
        
        private void OnEnable()
        {
            districtGenerator.OnDistrictCellBuilt += OnCellCollapsed;
        }

        private void OnDisable()
        {
            districtGenerator.OnDistrictCellBuilt -= OnCellCollapsed;
        }

        private void OnCellCollapsed(ChunkIndex index)
        {
            if (districtGenerator.SpawnedMeshes[index] is not PooledMonoBehaviour cellTransform)
            {
                return;
            }
            
            Vector3 targetScale = cellTransform.transform.localScale;
            cellTransform.transform.localScale = Vector3.zero;
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
                cellTransform.DOKill();
            }
        }
    }
}