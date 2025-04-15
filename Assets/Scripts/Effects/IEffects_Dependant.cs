using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Effects
{
    #region Money Effect

    public class AddMoneyEffect : IEffect 
    {
        [Title("Default Money")]
        [OdinSerialize]
        public float ModifierValue { get; set; } = 1;
        
        public void Perform(IAttacker unit)
        {
            Vector3 pos = unit.OriginPosition;
            MoneyManager.Instance.AddMoneyParticles(ModifierValue * unit.Stats.DamageMultiplier.Value, pos);
        }

        public void Revert(IAttacker unit)
        {
            MoneyManager.Instance.RemoveMoney(ModifierValue * unit.Stats.DamageMultiplier.Value);
        }
    }

    #endregion
}