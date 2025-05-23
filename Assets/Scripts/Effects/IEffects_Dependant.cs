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