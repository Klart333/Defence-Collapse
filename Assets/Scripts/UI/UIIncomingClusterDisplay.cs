using Buildings;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using DG.Tweening;
using UnityEngine;
using Enemy.ECS;
using Enemy;
using Juice;
using TMPro;

namespace UI
{
    public class UIIncomingClusterDisplay : PooledMonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Title("Setup")]
        [SerializeField]
        private TextMeshProUGUI turnText;
        
        [SerializeField]
        private TextMeshProUGUI enemyAmount;
        
        [SerializeField]
        private UIEnemyIcon enemyIcon;

        [SerializeField]
        private CanvasGroup fadeCanvasgroup;
        
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
        private Ease ease = Ease.InOutSine;
        
        private SelectedTileHandler selectedTileHandler;
        private Vector3 targetPosition;
        private Canvas canvas;
        private Camera cam;

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
        
        public void Display(SpawningComponent spawningComponent)
        {
            fadeCanvasgroup.DOKill();
            fadeCanvasgroup.alpha = 0;
            fadeCanvasgroup.DOFade(1f, 0.1f).SetEase(ease);
            
            EnemyUtility utility = spawningComponent.EnemyIndex >= 100 ? bossUtility : enemyUtility;
            int index = spawningComponent.EnemyIndex >= 100 ? spawningComponent.EnemyIndex - 100 : spawningComponent.EnemyIndex;
            EnemyData data = utility.GetEnemy(index);

            turnText.text = $"<u>{spawningComponent.Turns:N0}</u> Turns" ;
            enemyAmount.text = spawningComponent.Amount.ToString("N0");
            enemyIcon.DisplayEnemy(data);

            targetPosition = spawningComponent.Position;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            selectedCanvasGroup.DOKill();
            selectedCanvasGroup.DOFade(1, fadeDuration).SetEase(ease);
            selectedTileHandler.SelectTile(targetPosition, TileAction.None);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            selectedCanvasGroup.DOKill();
            selectedCanvasGroup.DOFade(0, fadeDuration).SetEase(ease);
            selectedTileHandler.Hide();
        }
    }
}