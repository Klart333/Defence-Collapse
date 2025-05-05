using Buildings.District;
using Gameplay.Money;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Loot
{
    public interface ILootEffect
    {
        void Perform(int grade);
    }

    [System.Serializable]
    public class GoldLoot : ILootEffect
    {
        [Title("Effect")]
        public int Gold = 10;

        [Title("Grade")]
        public float GradeMultiplier = 1;

        public void Perform(int grade)
        {
            MoneyManager.Instance.AddMoney(Gold + Mathf.RoundToInt(Gold * grade * GradeMultiplier));
        }
    }
    
    [System.Serializable]
    public class EffectLoot : ILootEffect
    {
        [Title("Effect")]
        public EffectModifier Effect = new EffectModifier();

        public void Perform(int grade)
        {
            EffectModifier effect = new EffectModifier(Effect);
            DistrictUpgradeManager.Instance.AddModifierEffect(effect);
            LootManager.Instance.DisplayEffectGained(effect);
        }
    }
}