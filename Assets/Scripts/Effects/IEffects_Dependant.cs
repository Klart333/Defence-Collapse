using System;
using Gameplay;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Gameplay.Money;
using Unity.Entities;
using UnityEngine;

namespace Effects
{
    #region Money Effect

    [Serializable]
    public class AddMoneyEffect : IEffect 
    {
        [Title("Default Money")]
        [OdinSerialize]
        public float ModifierValue { get; set; } = 1;

        [Title("Settings")]
        [SerializeField]
        private bool multiplyWithAttackSpeed;
        
        public bool IsDamageEffect => false;
        
        public void Perform(IAttacker unit)
        {
            Vector3 pos = unit.OriginPosition;
            float amount = ModifierValue * unit.Stats.Productivity.Value;
            if (multiplyWithAttackSpeed) amount *= unit.Stats.AttackSpeed.Value;
            
            MoneyManager.Instance.AddMoneyParticles(amount, pos);
        }

        public void Revert(IAttacker unit)
        {
            
        }
    }

    #endregion

    #region GameDataEffect
    
    [Serializable]
    public class GameDataEffect : IEffect 
    {
        [Title("GameData")]
        [OdinSerialize]
        private IComponentData componentData;
        
        public float ModifierValue { get; set; } = 1;
        public bool IsDamageEffect => false;
        
        public void Perform(IAttacker unit)
        {
            GameDataManager.Instance.IncreaseGameData(componentData);
        }

        public void Revert(IAttacker unit)
        {
            
        }
    }

    #endregion
}