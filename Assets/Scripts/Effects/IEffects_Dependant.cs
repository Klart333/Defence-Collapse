using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Gameplay.Money;
using Unity.Entities;
using Gameplay.Buffs;
using UnityEngine;
using Gameplay;
using System;
using System.Collections.Generic;

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

    #region GameData Effect
    
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
    
    #region Create Buff Effect

    [Serializable]
    public class CreateBuffEffect : IEffect 
    {
        [Title("Modifier Multiplier")]
        [OdinSerialize]
        public float ModifierValue { get; set; } = 1;

        [Title("Buff Settings")]
        [SerializeField]
        private Buff[] buffs;

        [SerializeField]
        private bool isPercentageIncrease = true;
        
        public bool IsDamageEffect => false;

        private Dictionary<IAttacker, List<BuffEmitter>> buffEmitters;
        
        public void Perform(IAttacker unit)
        {
            buffEmitters ??= new Dictionary<IAttacker, List<BuffEmitter>>();
            
            Vector3 pos = unit.OriginPosition;
            float range = unit.Stats.Range.Value;
            Buff[] copiedBuffs = new Buff[buffs.Length];
            for (int i = 0; i < copiedBuffs.Length; i++)
            {
                copiedBuffs[i] = new Buff(buffs[i]);
                if (isPercentageIncrease && copiedBuffs[i].Modifier.Type == Modifier.ModifierType.Multiplicative)
                {
                    copiedBuffs[i].Modifier.Value = 1.0f + buffs[i].Modifier.Value * ModifierValue * unit.Stats.Productivity.Value;
                }
                else
                {
                    copiedBuffs[i].Modifier.Value *= ModifierValue * unit.Stats.Productivity.Value;
                }
            }

            BuffEmitter buffEmitter = new BuffEmitter
            {
                Position = pos,
                RangeSquared = range * range,
                Buffs = copiedBuffs,
            };
            BuffEmitterManager.Instance.AddBuffEmitter(buffEmitter);
            
            if (buffEmitters.TryGetValue(unit, out List<BuffEmitter> list)) list.Add(buffEmitter);
            else buffEmitters.Add(unit, new List<BuffEmitter> { buffEmitter });
        }

        public void Revert(IAttacker unit)
        {
            if (!buffEmitters.TryGetValue(unit, out List<BuffEmitter> emitters))
            {
                return;
            }

            foreach (BuffEmitter buffEmitter in emitters)
            {
                BuffEmitterManager.Instance.RemoveBuffEmitter(buffEmitter);
            }
            buffEmitters.Remove(unit);
        }
    }

    #endregion

}