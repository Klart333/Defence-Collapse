using Sirenix.OdinInspector;
using Gameplay.Money;
using UnityEngine;
using Variables;
using Effects;
using TMPro;

namespace Gameplay
{
    public interface ITurnRewardEffect { }

    public interface ITurnRewardModifierEffect
    {
        public Modifier Perform(int turnAmount, int level);
        public void Revert(Modifier modifier);
    }

    public interface ITextEffect
    {
        public void SetText(TMP_Text text, params object[] arguments);
    }
    
    [System.Serializable]
    public class GoldMultiplierReward : ITurnRewardEffect, ITurnRewardModifierEffect, ITextEffect
    {
        [Title("Gold Multiplier")]
        [SerializeField]
        private float baseMultiplier = 1.0f;

        [SerializeField]
        private float baseTurnAmountMultiplierIncrease = 0.5f;
        
        [SerializeField]
        private float turnAmountMultiplierIncreaseLevelIncrease = 0.5f;

        [Title("Visual")]
        [SerializeField]
        private StringReference descriptionText;
        
        [SerializeField]
        private ColorReference textColor;
        
        public Modifier Perform(int turnAmount, int level)
        {
            float multiplier = GetMultiplier(turnAmount, level);
            Modifier modifier = new Modifier
            {
                Type = Modifier.ModifierType.Multiplicative,
                Value = multiplier,
            };
            
            MoneyManager.Instance.MoneyMultiplier.AddModifier(modifier);
            return modifier;
        }

        public void Revert(Modifier modifier)
        {
            MoneyManager.Instance.MoneyMultiplier.RemoveModifier(modifier);
        }

        public void SetText(TMP_Text text, params object[] arguments)
        {
            int turnAmount = (int)arguments[0];
            int level = (int)arguments[1];
            
            text.color = textColor.Value;
            text.text = descriptionText.Variable.LocalizedText.GetLocalizedString(GetMultiplier(turnAmount, level).ToString("F"));
        }

        private float GetMultiplier(int turnAmount, int level) => baseMultiplier + ((baseTurnAmountMultiplierIncrease + turnAmountMultiplierIncreaseLevelIncrease * level) * (turnAmount - 1));
    }
}