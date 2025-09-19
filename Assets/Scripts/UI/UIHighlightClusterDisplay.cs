using System.Collections.Generic;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using DG.Tweening;
using UnityEngine;
using Pathfinding;
using Enemy.ECS;
using Buildings;
using Enemy;
using Juice;
using TMPro;

namespace UI
{
    public class UIHighlightClusterDisplay : PooledMonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Title("Setup")]
        [SerializeField]
        private TextMeshProUGUI predictionText;
        
        [SerializeField]
        private TextMeshProUGUI enemyAmount;
        
        [SerializeField]
        private UIEnemyIcon enemyIcon;
        
        [SerializeField]
        private CanvasGroup fadeCanvasGroup;
        
        [SerializeField]
        private CanvasGroup selectedCanvasGroup;

        [SerializeField]
        private Vector2 pivot;
        
        [Title("References")]
        [SerializeField]
        private EnemyUtility enemyUtility;
        
        [SerializeField]
        private EnemyUtility bossUtility;
        
        [Title("Animation")]
        [SerializeField]
        private float fadeDuration = 0.2f;

        [SerializeField]
        private Ease fadeEase = Ease.InOutSine;
        
        private SelectedTileHandler selectedTileHandler;
        private Canvas canvas;
        private Camera cam;
        
        private HighlightClusterDataComponent highlightClusterData;
        private Vector3 targetPosition;
        
        private void Awake()
        {
            selectedTileHandler = FindFirstObjectByType<SelectedTileHandler>();
            canvas = GetComponentInParent<Canvas>();
            cam = Camera.main;
        }

        private void Update()
        {
            PositionRectTransform.PositionOnOverlayCanvas(canvas, cam, transform as RectTransform, targetPosition, pivot);
        }
        
        public void Display(HighlightClusterDataComponent highlightData)
        {
            fadeCanvasGroup.DOKill();
            fadeCanvasGroup.DOFade(1.0f, fadeDuration).SetEase(fadeEase);
         
            EnemyUtility utility = highlightData.EnemyType >= 100 ? bossUtility : enemyUtility;
            EnemyData data = utility.GetEnemy(highlightData.EnemyType);

            predictionText.text = highlightData.IsAttacking ? $"Will attack in <u>{highlightData.AttackTimer:N0}</u> turns!" : $"Will move in <u>{highlightData.MoveTimer:N0}</u> turns";
            
            enemyAmount.text = highlightData.EnemyAmount.ToString("N0");
            enemyIcon.DisplayEnemy(data);

            highlightClusterData = highlightData;
            targetPosition = PathUtility.GetPos(highlightData.PathIndex);
        }

        public void Hide()
        {
            fadeCanvasGroup.DOKill();
            fadeCanvasGroup.DOFade(0.0f, fadeDuration).SetEase(fadeEase).onComplete = () =>
            {
                gameObject.SetActive(false);
            };
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            selectedCanvasGroup.DOKill();
            selectedCanvasGroup.DOFade(1, fadeDuration).SetEase(fadeEase);
            
            if (highlightClusterData.IsAttacking)
            {
                selectedTileHandler.SelectTile(targetPosition, TileAction.None);
            }
            else
            {
                List<Vector3> tilePositions = GetMovementPrediction(highlightClusterData);
                selectedTileHandler.SelectTiles(tilePositions, TileAction.None);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            selectedCanvasGroup.DOKill();
            selectedCanvasGroup.DOFade(0, fadeDuration).SetEase(fadeEase);
            
            selectedTileHandler.Hide();
        }
        
        private List<Vector3> GetMovementPrediction(HighlightClusterDataComponent highlightData)
        {
            List<Vector3> positions = new List<Vector3> { targetPosition };
            int moves = math.max(1, (int)math.ceil(math.abs(highlightData.MoveTimer - 1) / highlightData.MovementSpeed));

            float3 movedPosition = targetPosition;
            PathIndex movedPathIndex = highlightData.PathIndex;
            while (moves-- > 0 && PathManager.Instance.TryMoveAlongFlowField(movedPathIndex, movedPosition, out movedPathIndex, out movedPosition))
            {
                positions.Add(movedPosition);
            }

            return positions;
        }

    }
}