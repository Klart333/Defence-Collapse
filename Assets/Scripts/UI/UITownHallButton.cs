using Sirenix.OdinInspector;
using Buildings.District.UI;
using Buildings.District;
using Gameplay.Event;
using DG.Tweening;
using UnityEngine;

namespace UI
{
    public class UITownHallButton : MonoBehaviour
    {
        [Title("Disabling")]
        [SerializeField]
        private GameObject[] disabledFoldoutObjects;

        [SerializeField]
        private CanvasGroup fadeInCanvasGroup;
        
        [Title("References")]
        [SerializeField]
        private GameObject townHallButton;
        
        [SerializeField]
        private TowerData townHallData;

        [SerializeField]
        private UIDistrictToggleButton townHallToggleButton;
        
        [Title("Animation")]
        [SerializeField]
        private float fadeInDuration = 0.5f;
        
        [SerializeField]
        private Ease fadeInEase = Ease.InOutSine;
        
        private void Awake()
        {
            townHallButton.SetActive(true);
            for (int i = 0; i < disabledFoldoutObjects.Length; i++)
            {
                disabledFoldoutObjects[i].SetActive(false);
            }
        }

        private void OnEnable()
        {
            Events.OnDistrictBuilt += OnDistrictBuilt;
            townHallToggleButton.OnClick += OnClick;
        }

        private void OnDisable()
        {
            Events.OnDistrictBuilt -= OnDistrictBuilt;
            townHallToggleButton.OnClick -= OnClick;
        }

        private void OnDistrictBuilt(TowerData towerData)
        {
            if (towerData.DistrictType is not DistrictType.TownHall)
            {
                return;
            }
            
            Events.OnDistrictBuilt -= OnDistrictBuilt;

            fadeInCanvasGroup.alpha = 0;
            fadeInCanvasGroup.DOFade(1.0f, fadeInDuration).SetEase(fadeInEase);
            townHallButton.SetActive(false);
            for (int i = 0; i < disabledFoldoutObjects.Length; i++)
            {
                disabledFoldoutObjects[i].SetActive(true);
            }
        }

        public void OnClick()
        {
            Events.OnDistrictClicked?.Invoke(townHallData);
        }
    }
}