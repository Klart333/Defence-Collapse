using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Gameplay;
using Sirenix.OdinInspector;
using UI;
using Unity.Collections;

namespace Buildings.District
{
    public class DistrictUnlockHandler : MonoBehaviour
    {
        [SerializeField]
        private TowerData[] unlockableTowers;
        
        [SerializeField]
        private UIDistrictUnlockPanel[] unlockPanels;
        
        [SerializeField]
        private GameObject canvasGameObject;
        
        [SerializeField]
        private CanvasGroup canvasGroup;
        
        [Title("Animation Settings")]
        [SerializeField]
        private float fadeInDuration = 1.0f;
        
        [SerializeField]
        private Ease fadeInEase = Ease.Linear;
        
        [SerializeField]
        private float fadeOutDuration = 1.0f;
        
        [SerializeField]
        private Ease fadeOutEase = Ease.Linear;

        private bool ignoreFirst = true;
        
        private readonly List<TowerData> unlockedTowers = new List<TowerData>();
        
        private void OnEnable()
        {
            Events.OnDistrictBuilt += OnDistrictBuilt;
        }

        private void OnDisable()
        {
            Events.OnDistrictBuilt -= OnDistrictBuilt;
        }

        private void OnDistrictBuilt(DistrictType districtType)
        {
            if (districtType != DistrictType.TownHall) return;
            if (ignoreFirst)
            {
                ignoreFirst = false;
                return;
            }

            List<TowerData> towers = new List<TowerData>(unlockableTowers);
            for (int i = 0; i < unlockedTowers.Count; i++)
            {
                towers.Remove(unlockedTowers[i]);
            }

            if (towers.Count <= 0)
            {
                return;
            }

            GameSpeedManager.Instance.SetGameSpeed(0.01f);
            canvasGroup.interactable = true;
            canvasGroup.DOKill();
            canvasGroup.alpha = 0;
            canvasGroup.DOFade(1.0f, fadeInDuration).SetEase(fadeInEase).timeScale = 1.0f / GameSpeedManager.Instance.Value;
            
            canvasGameObject.SetActive(true);
            for (int i = 0; i < unlockPanels.Length; i++)
            {
                if (towers.Count <= 0)
                {
                    unlockPanels[i].gameObject.SetActive(false);
                    continue;
                }
                
                int index = Random.Range(0, towers.Count);
                TowerData towerData = towers[index];
                towers.RemoveAtSwapBack(index);
                
                unlockPanels[i].DisplayDistrict(towerData, ChoseDistrict);
            }
        }

        public void ChoseDistrict(TowerData tower)
        {
            unlockedTowers.Add(tower);
            Events.OnDistrictUnlocked?.Invoke(tower);
            
            canvasGroup.interactable = false;
            canvasGroup.DOKill();

            GameSpeedManager.Instance.SetGameSpeed(1);
            canvasGroup.DOFade(0.0f, fadeOutDuration).SetEase(fadeOutEase).onComplete = () =>
            {
                canvasGameObject.SetActive(false);
            };
        }
    }
}