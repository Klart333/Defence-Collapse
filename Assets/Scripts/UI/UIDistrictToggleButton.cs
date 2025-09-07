using Buildings.District;
using UnityEngine.UI;
using UnityEngine;
using System;
using Gameplay.Event;

namespace UI
{
    public class UIDistrictToggleButton : MonoBehaviour
    {
        public event Action OnClick;
        
        [SerializeField]
        private Button districtButton;
        
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
                OnClick?.Invoke();
                isPlacing = true;
            }
            else
            {
                isPlacing = false;
                UIEvents.OnFocusChanged?.Invoke();
            }
        }

        private void OnPlacingCanceled()
        {
            if (!isPlacing) return;
            
            isPlacing = false;
        }
    }
}