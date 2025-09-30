using Sirenix.OdinInspector;
using Buildings.District;
using Gameplay.Event;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine;
using Variables;
using System;

namespace UI
{
    public class UIDistrictToggleButton : MonoBehaviour
    {
        public event Action OnClick;
        
        [Title("Setup")]
        [SerializeField]
        private Button districtButton;

        [Title("Visual")]
        [SerializeField]
        private Image visualElement;
        
        [SerializeField]
        private ColorReference defaultColor;
        
        [SerializeField]
        private ColorReference placingColor;
        
        [Title("Animation")]
        [SerializeField]
        private float colorAnimationDuration = 0.4f;
        
        [SerializeField]
        private Ease colorAnimationEase = Ease.InOutSine;
        
        private DistrictPlacer districtPlacer;
        
        private bool isPlacing;
        
        private void OnEnable()
        {
            districtPlacer = FindFirstObjectByType<DistrictPlacer>();
            districtPlacer.OnPlacingCanceled += OnPlacingCanceled;
        }
        
        private void OnDisable()
        {
            districtPlacer.OnPlacingCanceled -= OnPlacingCanceled;
        }

        public void OnButtonClicked()
        {
            if (!isPlacing)
            {
                SetIsPlacing();
            }
            else
            {
                SetNotPlacing();
            }
        }

        private void SetIsPlacing()
        {
            OnClick?.Invoke();
            
            isPlacing = true;
            visualElement.DOKill();
            visualElement.DOColor(placingColor.Value, colorAnimationDuration).SetEase(colorAnimationEase);
        }

        private void SetNotPlacing()
        {
            Hide();

            UIEvents.OnFocusChanged?.Invoke();
        }

        public void Hide()
        {
            isPlacing = false;
            visualElement.DOKill();
            visualElement.DOColor(defaultColor.Value, colorAnimationDuration).SetEase(colorAnimationEase);
        }

        private void OnPlacingCanceled()
        {
            if (!isPlacing) return;
            
            Hide();
        }
    }
}