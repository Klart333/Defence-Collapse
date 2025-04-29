using UnityEngine;
using System;
using Gameplay.Money;
using TMPro;
using WaveFunctionCollapse;

namespace Chunks
{
    public class ChunkUnlocker : PooledMonoBehaviour
    {
        public event Action OnChunkUnlocked;

        [SerializeField]
        private TextMeshProUGUI costText;

        [SerializeField]
        private Transform graphicParent;

        [SerializeField]
        private Vector2 pivot;

        private Camera cam;
        
        public int Cost { get; set; }
        public Canvas Canvas { get; set; }
        public Vector3 TargetPosition { get; set; }

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
            if (MoneyManager.Instance.Money < Cost)
            {
                MoneyManager.Instance.InsufficientFunds(Cost);
                return;
            }
            
            MoneyManager.Instance.RemoveMoney(Cost);
            OnChunkUnlocked?.Invoke();
            
            SetShowing(false);
        }

        public void DisplayCost()
        {
            costText.text = $"Unlock Cost: {Cost}";
        }

        public void SetShowing(bool value)
        {
            graphicParent.gameObject.SetActive(value);
        }
    }
}