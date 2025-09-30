using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using Gameplay.Event;
using DG.Tweening;
using UnityEngine;
using Variables;
using System;
using Unity.Mathematics;

namespace Buildings.District.DistrictLimit
{
    public class UIDistrictLimitDisplay : MonoBehaviour
    {
        [Title("Setup")]
        [SerializeField]
        private Image segmentPrefab;
        
        [SerializeField]
        private RectTransform segmentsContainer;

        [Title("Settings")]
        [SerializeField]
        private ColorReference segmentColor;
        
        [SerializeField]
        private ColorReference highlightedSegmentColor;

        [Title("Animation")]
        [SerializeField]
        private float totalAnimationDuration = 2.0f;

        [SerializeField]
        private float segmentColorAnimationDuration = 0.2f;
        
        [SerializeField]
        private Ease segmentColorAnimationEase = Ease.OutSine;
        
        private List<Image> spawnedSegments = new List<Image>();
        
        private DistrictLimitHandler limitHandler;
        
        private int currentSegment;
        
        private void OnDisable()
        {
            if (limitHandler)
            {
                limitHandler.DistrictsBuiltChanged -= OnDistrictsBuiltChanged;
            }
        }

        public void DisplaySegments(DistrictLimitHandler handler, int amount)
        {
            limitHandler = handler;
            
            float delay = (totalAnimationDuration - segmentColorAnimationDuration) / (amount - 1);
            for (int i = 0; i < amount; i++)
            {
                Image segment = Instantiate(segmentPrefab, segmentsContainer);
                AnimateColor(segment, segmentColor.Value, delay * i).Forget();
                
                spawnedSegments.Add(segment);
            }
            
            limitHandler.DistrictsBuiltChanged += OnDistrictsBuiltChanged;
        }
        
        private void OnDistrictsBuiltChanged(int changeAmount)
        {
            if (changeAmount > 0)
            {
                for (int i = 0; i < changeAmount; i++)
                {
                    IncreaseSegment();
                }
            }
            else
            {
                for (int i = 0; i < math.abs(changeAmount); i++)
                {
                    DecreaseSegment();
                }
            }
        }
        
        private void IncreaseSegment()
        {
            if (currentSegment >= spawnedSegments.Count)
            {
                return;
            }
            
            AnimateColor(spawnedSegments[currentSegment], highlightedSegmentColor.Value);
            currentSegment++;
        }
        
        private void DecreaseSegment()
        {
            if (currentSegment <= 0)
            {
                return;
            }
            
            currentSegment--;
            AnimateColor(spawnedSegments[currentSegment], segmentColor.Value);
        }

        private async UniTaskVoid AnimateColor(Image segment, Color targetColor, float delay)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay));

            AnimateColor(segment, targetColor);
        }
        
        private void AnimateColor(Image segment, Color targetColor)
        {
            segment.DOKill();
            segment.DOColor(targetColor, segmentColorAnimationDuration).SetEase(segmentColorAnimationEase);
        }
    }
}