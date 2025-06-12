using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Exp.Gemstones
{
    public class UISellGemstone : MonoBehaviour
    {
        [Title("References")]
        [SerializeField]
        private UIGemstoneContainer gemstoneContainer;
        
        [SerializeField]
        private TextMeshProUGUI sellText;

        [Title("Exp")]
        [SerializeField]
        private AnimationCurve levelToExp;
        
        private ExpManager expManager;

        private void OnEnable()
        {
            GetExpManager().Forget();
            
            gemstoneContainer.OnGemstoneEnter += OnGemstoneEnter;
        }

        private void OnDisable()
        {
            gemstoneContainer.OnGemstoneEnter -= OnGemstoneEnter;
        }

        private void OnGemstoneEnter(Gemstone stone)
        {
            int amount = (int)levelToExp.Evaluate(stone.Level);
            sellText.text = $"+{amount:N0} Exp";
        }

        private async UniTaskVoid GetExpManager()
        {
            expManager = await ExpManager.Get();
        }

        public void SellGemstone()
        {
            if (gemstoneContainer.transform.childCount != 1)
            {
                return;
            }

            if (!gemstoneContainer.transform.GetChild(0).gameObject.TryGetComponent(out UIGemstone stone))
            {
                return;
            }

            int exp = (int)levelToExp.Evaluate(stone.Gemstone.Level);
            
            expManager.AddExp(exp);
            gemstoneContainer.RemoveGemstone(stone);
            
            Destroy(stone.gameObject);
        }
    }
}