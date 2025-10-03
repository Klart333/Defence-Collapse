using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using DG.Tweening;
using UnityEngine;
using Juice;
using TMPro;
using UI;

namespace Buildings.District.UI
{
    public class UIDistrictDisplay : PooledMonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Title("Setup")]
        [SerializeField]
        private TextMeshProUGUI predictionText;
        
        [SerializeField]
        private CanvasGroup fadeCanvasGroup;
        
        [SerializeField]
        private CanvasGroup selectedCanvasGroup;

        [SerializeField]
        private Vector2 pivot;
        
        [Title("Animation")]
        [SerializeField]
        private float fadeDuration = 0.2f;

        [SerializeField]
        private Ease fadeEase = Ease.InOutSine;
        
        private SelectedTileHandler selectedTileHandler;
        private Canvas canvas;
        private Camera cam;
        
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
        
        public void Display(DistrictData districtData)
        {
            fadeCanvasGroup.DOKill();
            fadeCanvasGroup.DOFade(1.0f, fadeDuration).SetEase(fadeEase);

            predictionText.text = "smth";
            
            targetPosition = districtData.Position;
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
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            selectedCanvasGroup.DOKill();
            selectedCanvasGroup.DOFade(0, fadeDuration).SetEase(fadeEase);
            
            selectedTileHandler.Hide();
        }
    }
}