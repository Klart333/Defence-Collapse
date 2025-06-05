using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using DG.Tweening;
using UnityEngine;
using System;
using TMPro;
using Exp;

namespace Gameplay.GameOver
{
    public class UIGameOverExpHandler : MonoBehaviour
    {
        [Title("References")]
        [SerializeField]
        private TextMeshProUGUI expText;

        [SerializeField]
        private TextMeshProUGUI expTextFactorPrefab;
        
        [SerializeField]
        private Transform expTextParent;

        [Title("Factors")]
        [SerializeField, Tooltip("Added sequentially, so put mutlipliers at the end")]
        private ExpFactor[] expFactors;
        
        [Title("Display Animation")]
        [SerializeField]
        private Ease fadeInFactorEase = Ease.InCirc;
        
        [SerializeField]
        private float fadeInFactorDuration = 0.5f;
        
        [SerializeField]
        private Ease textCountingEase = Ease.InOutCirc;
        
        [SerializeField]
        private float textCountingDuration = 0.2f;
        
        public int ExpGained { get; private set; }
        
        private void OnEnable()
        {
            CalculateExp();
            AnimateDisplayExp().Forget();
        }

        private void CalculateExp()
        {
            float totalExp = 0;
            for (int i = 0; i < expFactors.Length; i++)
            {
                float exp = expFactors[i].GetFactor(out _);
                totalExp = expFactors[i].FactorType switch
                {
                    FactorType.Exp => totalExp + exp,
                    FactorType.Multiplier when exp > 1 => totalExp * exp,
                    _ => totalExp
                };
            }
            
            ExpGained = (int)totalExp;
            ExpManager.Instance.AddExp(ExpGained);
        }

        private async UniTaskVoid AnimateDisplayExp()
        {
            await UniTask.Delay(1000);
            
            float totalExp = 0;
            for (int i = 0; i < expFactors.Length; i++)
            {
                float currentExp = totalExp;
                float exp = expFactors[i].GetFactor(out float level);
                totalExp = expFactors[i].FactorType switch
                {
                    FactorType.Exp => totalExp + exp,
                    FactorType.Multiplier when exp > 1 => totalExp * exp,
                    _ => totalExp
                };

                switch (expFactors[i].FactorType)
                {
                    case FactorType.Exp when exp <= 0:
                    case FactorType.Multiplier when exp <= 1:
                        continue;
                }
                
                TextMeshProUGUI factorText = Instantiate(expTextFactorPrefab, expTextParent);
                float targetHeight = factorText.rectTransform.rect.height;
                factorText.rectTransform.sizeDelta = new Vector2(factorText.rectTransform.sizeDelta.x, 0);
                factorText.rectTransform.DOSizeDelta(new Vector2(factorText.rectTransform.sizeDelta.x, targetHeight), fadeInFactorDuration * 0.6f);
                factorText.transform.SetAsFirstSibling();
                string format = expFactors[i].FactorType switch
                {
                    FactorType.Multiplier => "N",
                    _ => "N0",
                };
                factorText.text = string.Format(expFactors[i].DisplayText, expFactors[i].GetDisplayLevel(level), exp.ToString(format));
                
                CanvasGroup canvasGroup = factorText.GetComponent<CanvasGroup>();
                canvasGroup.DOFade(1, fadeInFactorDuration).SetEase(fadeInFactorEase);
                await UniTask.Delay(TimeSpan.FromSeconds(fadeInFactorDuration * 0.9f));
                
                DOTween.To(x => expText.text = $"Exp: {x:N0}", currentExp, totalExp, textCountingDuration).SetEase(textCountingEase);
                await UniTask.Delay(TimeSpan.FromSeconds(textCountingDuration * 1f));
                expText.transform.DOPunchScale(Vector3.one * 0.05f, 0.1f);;
            }

            await UniTask.Delay(TimeSpan.FromSeconds(textCountingDuration * 0.15f));
            expText.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f);;
        }
    }
}