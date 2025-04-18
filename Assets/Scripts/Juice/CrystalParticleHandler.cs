using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using Unity.Collections;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

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
        
        private readonly List<Tuple<PooledMonoBehaviour, Vector3>> spawnedCrystals = new List<Tuple<PooledMonoBehaviour, Vector3>>();

        private Camera cam;

        private void Start()
        {
            cam = Camera.main;
        }

        private void Update()
        {
            int count = spawnedCrystals.Count;
            if (count == 0)
            {
                return;
            }
            
            Vector3 targetPosition = UICrystalTarget.position;
            for (int i = count - 1; i >= 0; i--)
            {
                float value = (spawnedCrystals[i].Item1.Delay.Lifetime - spawnedCrystals[i].Item1.Delay.LifeLeft) / spawnedCrystals[i].Item1.Delay.Lifetime;
                spawnedCrystals[i].Item1.transform.position = Vector3.Lerp(spawnedCrystals[i].Item2, targetPosition, Mathf.SmoothStep(0.0f, 1.0f, value));

                if (value >= 1.0f)
                {
                    spawnedCrystals.RemoveAtSwapBack(i);
                }
            }
        }

        public async UniTaskVoid CollectCrystals(float moneyAmount, Vector3 origin)
        {
            int amount = (int)moneyAmount;
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
                MoneyManager.Instance.AddMoney(moneyShare);
            }

            async UniTaskVoid SetDoScale(PooledMonoBehaviour spawned, float duration)
            {
                if (!spawned.TryGetComponent(out AnimatedPopup popup)) return;
                
                Stopwatch timer = Stopwatch.StartNew();
                await popup.PopupTween.AsyncWaitForCompletion();
                timer.Stop();
                spawned.transform.DOScale(Vector3.zero, duration - (float)timer.Elapsed.TotalSeconds).SetEase(Ease.InSine);
            }
        }
    }
}