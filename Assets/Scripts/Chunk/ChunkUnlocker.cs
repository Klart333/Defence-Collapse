using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using UnityEngine.Events;
using UnityEngine.UI;
using Gameplay.Money;
using UnityEngine;
using System;
using TMPro;

namespace Chunks
{
    public class ChunkUnlocker : PooledMonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        public event Action OnChunkUnlocked;

        [Title("References")]
        [SerializeField]
        private Image selectedImage;

        [SerializeField]
        private TextMeshProUGUI costText;

        [SerializeField]
        private Transform graphicParent;

        [SerializeField]
        private Vector2 pivot;
        
        [Title("Events")]
        [SerializeField]
        private UnityEvent onUnlocked;
        
        private Camera cam;
        
        public Vector3 TargetPosition { get; set; }
        public Canvas Canvas { get; set; }
        public float Cost { get; set; }

        private void Awake()
        {
            cam = Camera.main;
        }

        private void Update()
        {
            if (graphicParent.gameObject.activeInHierarchy)
            {
                PositionRectTransform.PositionOnOverlayCanvas(Canvas, cam, transform as RectTransform, TargetPosition, pivot);
            }
        }

        public void Unlock()
        {
            MoneyManager.Instance.RemoveMoney(Cost);
            OnChunkUnlocked?.Invoke();
            
            SetShowing(false);
        }

        public void DisplayCost()
        {
            costText.text = $"Unlock Cost: {Cost:N0}";
        }

        public void SetShowing(bool value)
        {
            graphicParent.gameObject.SetActive(value);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            selectedImage.gameObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            selectedImage.gameObject.SetActive(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (MoneyManager.Instance.Money < Cost)
            {
                MoneyManager.Instance.InsufficientFunds(Cost);
                return;
            }
            
            Unlock();
            onUnlocked?.Invoke();
        }
    }
}