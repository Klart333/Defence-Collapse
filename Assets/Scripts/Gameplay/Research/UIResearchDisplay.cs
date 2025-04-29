using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Gameplay.Research
{
    public class UIResearchDisplay : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI amountText;

        private async void OnEnable()
        {
            await UniTask.WaitUntil(() => ResearchManager.Instance != null);
            ResearchManager.Instance.OnResearchChanged += OnResearchChanged;
        }

        private void OnDisable()
        {
            ResearchManager.Instance.OnResearchChanged -= OnResearchChanged;
        }

        private void OnResearchChanged()
        {
            amountText.text = ResearchManager.Instance.ResearchCurrency.ToString("N0");
        }
    }
}