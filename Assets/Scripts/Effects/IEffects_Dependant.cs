using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Gameplay.Money;
using UnityEngine;

namespace Effects
{
    #region Money Effect

    public class AddMoneyEffect : IEffect 
    {
        [Title("Default Money")]
        [OdinSerialize]
        public float ModifierValue { get; set; } = 1;
        
        public bool IsDamageEffect => false;
        
        public void Perform(IAttacker unit)
        {
            Vector3 pos = unit.OriginPosition;
            MoneyManager.Instance.AddMoneyParticles(ModifierValue * unit.Stats.Productivity.Value, pos);
        }

        public void Revert(IAttacker unit)
        {
            
        }
    }

    #endregion
}