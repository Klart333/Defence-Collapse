using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Gameplay.Money;
using Unity.Entities;
using Gameplay.Buffs;
using UnityEngine;
using Gameplay;
using System;
using System.Collections.Generic;
using Exp.Gemstones;
using Gameplay.Upgrades;
using Object = UnityEngine.Object;

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
        
        public void Perform(IAttacker unit)
        {
            Vector3 pos = unit.OriginPosition;
            float amount = ModifierValue * unit.Stats.Productivity.Value * MoneyManager.Instance.MoneyMultiplier.Value;
            if (multiplyWithAttackSpeed) amount *= unit.Stats.AttackSpeed.Value;
            
            MoneyManager.Instance.AddMoneyParticles(amount, pos);

            if (unit is IAttackerStatistics stats)
            {
                stats.GoldGained += amount;
            }
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
        
        public void Perform(IAttacker unit)
        {
            GameDataManager.Instance.IncreaseGameData(componentData);
        }
    }

    #endregion
    
    #region Create Buff Effect

    [Serializable]
    public class CreateBuffEffect : IEffect, IRevertableEffect
    {
        [Title("Modifier Multiplier")]
        [OdinSerialize]
        public float ModifierValue { get; set; } = 1;

        [Title("Buff Settings")]
        [SerializeField]
        private Buff[] buffs;

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
                copiedBuffs[i].Modifier.Value *= ModifierValue * unit.Stats.Productivity.Value;
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
    
    #region Gemstone Effect

    [Serializable]
    public class GemstoneEffect : IEffect 
    {
        [Title("Modifier Multiplier")]
        [OdinSerialize]
        public float ModifierValue { get; set; } = 1;

        [Title("Gemstone")]
        [OdinSerialize]
        private IGemstoneEffect[] gemstoneEffects;
        
        public void Perform(IAttacker unit)
        {
            if (unit != null)
            {
                Debug.Log("Why are you sending a unit here?");
            }

            for (int i = 0; i < gemstoneEffects.Length; i++)
            {
                gemstoneEffects[i].PerformEffect();
            }
        }
    }

    #endregion
}