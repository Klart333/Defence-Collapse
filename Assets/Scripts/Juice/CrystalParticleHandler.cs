using Random = UnityEngine.Random;

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using System.Diagnostics;
using Unity.Collections;
using Unity.Mathematics;
using Gameplay.Money;
using DG.Tweening;
using UnityEngine;
using Gameplay;
using System;
using Debug = UnityEngine.Debug;

namespace Juice
{
    public class CrystalParticleHandler : MonoBehaviour
    {
        [Title("References")]
        [SerializeField]
        private PooledMonoBehaviour crystalPrefab;

        [SerializeField]
        private RectTransform UICrystalTarget;
        
        [Title("Settings")]
        [SerializeField]
        private float randomPositionDistance = 0.05f;

        [SerializeField]
        private float rotationStrength = 2;
        
        [SerializeField, MinMaxRange(0, 5)]
        private RangedFloat durationRange = new RangedFloat(0.1f, 5);

        [SerializeField, MinMaxRange(0, 1)]
        private RangedFloat spawnDuration = new RangedFloat(0.1f, 1.0f);
        
        private List<Tuple<PooledMonoBehaviour, Vector3>> spawnedCrystals = new List<Tuple<PooledMonoBehaviour, Vector3>>();

        private IGameSpeed gameSpeed;
        
        private Camera cam;

        private void Start()
        {
            cam = Camera.main;
            gameSpeed = GameSpeedManager.Instance;
        }

        private void Update()
        {
            int count = spawnedCrystals.Count;
            if (count == 0)
            {
                return;
            }
            
            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(null, UICrystalTarget.position);
            screenPos.z = 1;
            Vector3 targetPosition = cam.ScreenToWorldPoint(screenPos);
            for (int i = count - 1; i >= 0; i--)
            {
                float value = (spawnedCrystals[i].Item1.Delay.Lifetime - spawnedCrystals[i].Item1.Delay.LifeLeft) / spawnedCrystals[i].Item1.Delay.Lifetime;
                spawnedCrystals[i].Item1.transform.position = math.lerp(spawnedCrystals[i].Item2, targetPosition, math.smoothstep(0.0f, 1.0f, value));

                if (value >= 1.0f)
                {
                    spawnedCrystals.RemoveAtSwapBack(i);
                }
            }
        }

        public async UniTaskVoid CollectCrystals(float moneyAmount, Vector3 origin)
        {
            int amount = (int)math.log2(moneyAmount);
            if (amount <= 0)
            {
                MoneyManager.Instance.AddMoney(moneyAmount);
                return;
            }
            
            float moneyShare = moneyAmount / amount;

            float delay = amount > 1 
                ? spawnDuration.Random() / amount
                : 0;
            for (int i = 0; i < amount; i++)
            {
                Vector3 pos = origin + Random.onUnitSphere * (randomPositionDistance * Mathf.Log10(amount));
                Quaternion rot = Quaternion.LookRotation(Random.onUnitSphere);
                PooledMonoBehaviour spawned = crystalPrefab.GetAtPosAndRot<PooledMonoBehaviour>(pos, rot);
                float duration = durationRange.Random();
                spawned.Delay.Lifetime = duration;
                
                Vector3 rotation = Random.onUnitSphere * rotationStrength;
                spawned.transform.DOBlendableRotateBy(rotation, duration);
                SetDoScale(spawned, duration).Forget();
                
                spawnedCrystals.Add(Tuple.Create(spawned, pos));
                
                spawned.OnReturnToPool += SpawnedOnOnReturnToPool;

                await UniTask.Delay(TimeSpan.FromSeconds(delay));
            }
            
            void SpawnedOnOnReturnToPool(PooledMonoBehaviour spawned)
            {
                spawned.OnReturnToPool -= SpawnedOnOnReturnToPool;
                spawned.DOKill();
                MoneyManager.Instance.AddMoney(moneyShare, false);
            }

            async UniTaskVoid SetDoScale(PooledMonoBehaviour spawned, float duration)
            {
                if (!spawned.TryGetComponent(out AnimatedPopup popup)) return;
                
                Stopwatch timer = Stopwatch.StartNew();
                await popup.PopupTween.AsyncWaitForCompletion();
                timer.Stop();
                spawned.transform.DOScale(Vector3.zero, duration - (float)timer.Elapsed.TotalSeconds).SetEase(Ease.InSine).ScaleWithGameSpeed(gameSpeed);
            }
        }
    }
}