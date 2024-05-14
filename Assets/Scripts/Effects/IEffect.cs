using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Effects
{
    public interface IEffect
    {
        public void Perform(IAttacker attacker);
        public void Revert(IAttacker attacker);

        public float ModifierValue { get; set; }
    }

    public interface IEffectHolder : IEffect 
    {
        public List<IEffect> Effects { get; set; }

        public IEffectHolder Clone();
    }

    #region Increase Stat

    public class IncreaseStatEffect : IEffect
    {
        [TitleGroup("Modifier")]
        [OdinSerialize]
        public float ModifierValue { get; set; } = 20f;

        [TitleGroup("Modifier")]
        public Modifier.ModifierType ModifierType;

        [TitleGroup("Modifier")]
        public StatType StatType;

        [TitleGroup("Options")]
        public bool CanIncrease = true;

        private Dictionary<IAttacker, Modifier> ModifierDictionary;

        public void Perform(IAttacker unit)
        {
            if (ModifierDictionary == null)
            {
                ModifierDictionary = new Dictionary<IAttacker, Modifier>();
            }

            if (!ModifierDictionary.ContainsKey(unit))
            {
                ModifierDictionary.Add(unit, new Modifier
                {
                    Type = ModifierType,
                    Value = ModifierValue
                });

                unit.Stats.ModifyStat(StatType, ModifierDictionary[unit]);
            }
            else if (CanIncrease)
            {
                ModifierDictionary[unit].Value += ModifierValue;
            }
        }

        public void Revert(IAttacker unit)
        {
            if (ModifierDictionary == null || !ModifierDictionary.ContainsKey(unit))
            {
                return;
            }

            unit.Stats.RevertModifiedStat(StatType, ModifierDictionary[unit]);

            ModifierDictionary.Remove(unit);
        }
    }

    #endregion

    #region Timed Stat Increase

    public class TimedStatIncreaseEffect : IEffect
    {
        [Title("Increase Amount")]
        [OdinSerialize]
        public float ModifierValue { get; set; } = 1f;

        [Title("Stat")]
        public Modifier.ModifierType ModifierType;

        public StatType StatType;

        public float Time = 3;

        private Dictionary<IAttacker, Modifier> ModifierDictionary;

        public async void Perform(IAttacker unit)
        {
            if (ModifierDictionary == null)
            {
                ModifierDictionary = new Dictionary<IAttacker, Modifier>();
            }

            if (ModifierDictionary.ContainsKey(unit))
            {
                ModifierDictionary[unit].Value += ModifierValue;
            }
            else
            {
                ModifierDictionary.Add(unit, new Modifier
                {
                    Type = ModifierType,
                    Value = ModifierValue
                });

                unit.Stats.ModifyStat(StatType, ModifierDictionary[unit]);
            }
            float originalValue = ModifierDictionary[unit].Value;

            await UniTask.Delay(TimeSpan.FromSeconds(Time + originalValue / 100.0f));

            if (unit == null)
            {
                if (ModifierDictionary.ContainsKey(unit))
                {
                    ModifierDictionary.Remove(unit);
                }

                return;
            }

            if (ModifierDictionary[unit].Value != originalValue) // A different instance will revert it
            {
                return;
            }

            Revert(unit);
        }

        public void Revert(IAttacker unit)
        {
            if (ModifierDictionary == null || !ModifierDictionary.ContainsKey(unit))
            {
                return;
            }

            unit.Stats.RevertModifiedStat(StatType, ModifierDictionary[unit]);

            ModifierDictionary.Remove(unit);
        }
    }

    #endregion

    #region Temporary Increase Stat

    public class TemporaryIncreaseStatEffect : IEffect
    {
        [TitleGroup("Modifier")]
        [OdinSerialize]
        public float ModifierValue { get; set; } = 20f;

        [TitleGroup("Modifier")]
        public Modifier.ModifierType ModifierType;

        [TitleGroup("Modifier")]
        public StatType StatType;

        [TitleGroup("Modifier")]
        public float ChanceToTrigger = 0.2f;

        private HashSet<IAttacker> unitsAttacking = new HashSet<IAttacker>();

        public void Perform(IAttacker unit)
        {
            if (unitsAttacking == null) unitsAttacking = new HashSet<IAttacker>();

            if (unitsAttacking.Contains(unit)) return;

            unit.Stats.ModifyStat(StatType, new Modifier
            {
                Type = ModifierType,
                Value = ModifierValue
            });

            Action RevertAfterAttack = null;
            RevertAfterAttack = () =>
            {
                Revert(unit);
                unit.OnAttack -= RevertAfterAttack;
            };

            unit.OnAttack += RevertAfterAttack;
            unitsAttacking.Add(unit);
        }

        public async void Revert(IAttacker unit)
        {
            if (unitsAttacking == null)
            {
                return;
            }

            unit.Stats.RevertModifiedStat(StatType, new Modifier
            {
                Type = ModifierType,
                Value = ModifierValue
            });

            await UniTask.NextFrame();
            unitsAttacking.Remove(unit);
        }
    }

    #endregion

    #region Damage Collider

    public class DamageColliderEffect : IEffect
    {
        [Title("Attack Damage")]
        [OdinSerialize]
        public float ModifierValue { get; set; } = 20f;

        [Title("Hits")]
        public bool LimitedHits = false;
        [ShowIf(nameof(LimitedHits))]
        public int Hits = 0;

        [Title("Collider")]
        public float Radius = 1;

        public void Perform(IAttacker unit)
        {
            Vector3 pos = unit.AttackPosition;

            DamageInstance dmg = new DamageInstance
            {
                Damage = ModifierValue * unit.Stats.DamageMultiplier.Value,
                Source = unit
            };

            DamageCollider collider = ColliderManager.Instance.GetCollider(pos, Radius, layermask: unit.LayerMask);
            collider.Attacker = unit;
            collider.DamageInstance = dmg;

            if (LimitedHits)
            {
                int hits = 0;
                Action LimitHitAction = null;
                LimitHitAction = () =>
                {
                    if (++hits >= Hits)
                    {
                        collider.OnHit -= LimitHitAction;
                        collider.gameObject.SetActive(false);
                    }
                };

                collider.OnHit += LimitHitAction;
            }
        }

        public void Revert(IAttacker unit)
        {
            // Cant revert
        }
    }

    #endregion

    #region Arched Damage Collider

    public class ArchedDamageColliderEffect : IEffect
    {
        [Title("Attack Damage")]
        [OdinSerialize]
        public float ModifierValue { get; set; } = 20f;

        [Title("Hits")]
        public bool LimitedHits = false;
        [ShowIf(nameof(LimitedHits))]
        public int Hits = 0;

        [Title("Collider")]
        public float Radius = 1;

        [Title("Movement")]
        public float Height = 5;
        public float UnitsPerSecond = 8;
        public Ease Ease;

        [Title("Visual")]
        public AttackVisualEffect AttackEffect;

        public void Perform(IAttacker attacker)
        {
            DamageInstance dmg = new DamageInstance
            {
                Damage = ModifierValue * attacker.Stats.DamageMultiplier.Value,
                Source = attacker
            };

            Vector3 targetPosition = attacker.AttackPosition;

            float distance = Vector3.Distance(attacker.OriginPosition, targetPosition) + Height;
            float lifetime = distance / UnitsPerSecond;

            AttackVisualEffect visual = AttackEffect.Spawn(attacker.OriginPosition, Quaternion.identity, 1, lifetime + 0.2f);
            DamageCollider collider = ColliderManager.Instance.GetCollider(attacker.OriginPosition, Radius, lifetime + 0.1f, layermask: attacker.LayerMask);
            collider.Attacker = attacker;
            collider.DamageInstance = dmg;

            float t = 0;
            var tween = DOTween.To(() => t, (t) =>
            {
                Vector3 pos = GetPosition(attacker.OriginPosition, targetPosition, t);
                collider.transform.position = pos;
                visual.transform.position = pos;
            }, 1f, lifetime).SetEase(Ease);

            if (LimitedHits)
            {
                int hits = 0;
                Action LimitHitAction = null;
                LimitHitAction = () =>
                {
                    if (++hits >= Hits)
                    {
                        tween.Kill();
                        collider.OnHit -= LimitHitAction;

                        collider.gameObject.SetActive(false);
                        visual.OnAttackBreak();
                    }
                };

                collider.OnHit += LimitHitAction;
            }
        }

        private Vector3 GetPosition(Vector3 start, Vector3 target, float t)
        {
            Vector3 midPos = Vector3.Lerp(start, target, 0.5f) + Vector3.up * Height;
            return Vector3.Lerp(Vector3.Lerp(start, midPos, t), Vector3.Lerp(midPos, target, t), t);
        }

        public void Revert(IAttacker unit)
        {
            // Cant revert
        }
    }

    #endregion

    #region Stacking Effect

    public class StackingEffectEffect : IEffect
    {
        [TitleGroup("Stat Increase")]
        [OdinSerialize]
        public float ModifierValue { get; set; } = 1f;

        [TitleGroup("Stat Increase")]
        public bool ShouldMultiply = false;

        [Title("Effect")]
        [OdinSerialize]
        public IEffect EffectToStack;

        private Dictionary<IAttacker, float> MultiplierDictionary;

        public void Perform(IAttacker unit)
        {
            if (MultiplierDictionary == null)
            {
                MultiplierDictionary = new Dictionary<IAttacker, float>();
            }

            if (!MultiplierDictionary.ContainsKey(unit))
            {
                if (ShouldMultiply)
                {
                    MultiplierDictionary.Add(unit, EffectToStack.ModifierValue);
                }
                else
                {
                    MultiplierDictionary.Add(unit, ModifierValue);
                }
            }
            else
            {
                if (ShouldMultiply)
                {
                    MultiplierDictionary[unit] *= this.ModifierValue;
                }
                else
                {
                    MultiplierDictionary[unit] += this.ModifierValue;
                }
            }

            float value = EffectToStack.ModifierValue;

            EffectToStack.ModifierValue = MultiplierDictionary[unit];
            EffectToStack.Perform(unit);

            EffectToStack.ModifierValue = value;
        }

        public void Revert(IAttacker unit)
        {
            if (MultiplierDictionary == null || !MultiplierDictionary.ContainsKey(unit))
            {
                return;
            }

            float value = EffectToStack.ModifierValue;

            EffectToStack.ModifierValue = MultiplierDictionary[unit];
            EffectToStack.Revert(unit);

            EffectToStack.ModifierValue = value;

            MultiplierDictionary.Remove(unit);
        }
    }

    #endregion

    #region Damage Over Time On Damage

    public class DamageOverTimeOnDamageEffect : IEffect
    {
        [TitleGroup("Percent Damage DOT'd")]
        [OdinSerialize]
        public float ModifierValue { get; set; }

        [TitleGroup("Percent Damage DOT'd")]
        public float TimePeriod = 0;

        private const float tickRate = 0.2f;
        private const int EffectKey = 150;

        public async void Perform(IAttacker unit)
        {
            DamageInstance damageToDOT = unit.LastDamageDone;

            if (damageToDOT == null || damageToDOT.SpecialEffectSet.Contains(EffectKey))
            {
                return;
            }

            int ticks = Mathf.FloorToInt(Mathf.Max(1, TimePeriod / tickRate));
            float totalDamage = (damageToDOT.Damage) * ModifierValue;
            float damage = totalDamage / ticks;
            //Debug.Log(tickRate + ", Triggering DamageOverTime with " + damageToDOT.AbilityDamage + ", that is first multiplied with " + ModifierValue + " resulting in " + totalDamage + ", then that is divided by " + ticks + " finally resulting in " + damage); ;

            DamageInstance dotInstance = new DamageInstance
            {
                Source = unit,
                Damage = damage,
                TargetHit = damageToDOT.TargetHit,
                CritMultiplier = unit.Stats.GetCritMultiplier(),
                SpecialEffectSet = damageToDOT.SpecialEffectSet,
            };

            dotInstance.SpecialEffectSet.Add(EffectKey);

            for (int i = 0; i < ticks; i++)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(tickRate));

                dotInstance.TargetHit.TakeDamage(dotInstance, out DamageInstance damageDone);

                if (!damageDone.SpecialEffectSet.Contains(EffectKey))
                {
                    damageDone.SpecialEffectSet.Add(EffectKey);
                }
                unit.OnUnitDoneDamage(damageDone);
            }
        }

        public void Revert(IAttacker unit)
        {
            // Nothing to revert
        }
    }

    #endregion

    #region Random Bonus

    public class RandomBonusEffect : IEffect
    {
        [TitleGroup("Modifier")]
        [OdinSerialize]
        public float ModifierValue { get; set; } = 2f;

        [TitleGroup("Modifier")]
        public Modifier.ModifierType ModifierType;

        [TitleGroup("Modifier")]
        public int StatAmount;

        [TitleGroup("Options")]
        public bool CanIncrease = true;

        private Dictionary<IAttacker, List<(StatType, Modifier)>> ModifierDictionary;

        public void Perform(IAttacker unit)
        {
            if (ModifierDictionary == null)
            {
                ModifierDictionary = new Dictionary<IAttacker, List<(StatType, Modifier)>>();
            }

            if (!ModifierDictionary.ContainsKey(unit))
            {
                ModifierDictionary.Add(unit, new List<(StatType, Modifier)>());

                for (int i = 0; i < StatAmount; i++)
                {
                    int statTypeIndex = UnityEngine.Random.Range(0, Enum.GetValues(typeof(StatType)).Length);
                    Modifier modifier = new Modifier
                    {
                        Type = ModifierType,
                        Value = ModifierValue
                    };

                    unit.Stats.ModifyStat((StatType)statTypeIndex, modifier);

                    ModifierDictionary[unit].Add(((StatType)statTypeIndex, modifier));
                }

            }
            else if (CanIncrease)
            {
                //ModifierDictionary[unit].Value += ModifierValue;
            }
        }

        public void Revert(IAttacker unit)
        {
            if (ModifierDictionary == null || !ModifierDictionary.ContainsKey(unit))
            {
                return;
            }

            for (int i = 0; i < ModifierDictionary[unit].Count; i++)
            {
                unit.Stats.RevertModifiedStat(ModifierDictionary[unit][i].Item1, ModifierDictionary[unit][i].Item2);
            }

            ModifierDictionary.Remove(unit);
        }
    }

    #endregion

    #region Health Condition

    public class HealthConditionalEffect : IEffect
    {
        [TitleGroup("Health Percent")]
        [OdinSerialize]
        public float ModifierValue { get; set; } = 0.5f;

        [TitleGroup("Health Percent")]
        [OdinSerialize]
        private IEffect EffectToTrigger;

        [TitleGroup("Health Percent")]
        public bool TriggerOnce = true;

        private HashSet<IAttacker> TriggeredUnits;

        public void Perform(IAttacker unit)
        {
            if (TriggeredUnits == null)
            {
                TriggeredUnits = new HashSet<IAttacker>();
            }

            if (TriggerOnce && TriggeredUnits.Contains(unit))
            {
                return;
            }

            if (unit.Health.HealthPercentage <= ModifierValue)
            {
                EffectToTrigger.Perform(unit);

                if (TriggerOnce)
                {
                    TriggeredUnits.Add(unit);
                }
            }
        }

        public void Revert(IAttacker unit)
        {
            if (TriggeredUnits == null)
            {
                return;
            }

            if (TriggerOnce)
            {
                if (!TriggeredUnits.Contains(unit))
                {
                    return;
                }

                TriggeredUnits.Remove(unit);
            }

            EffectToTrigger.Revert(unit);
        }
    }

    #endregion

    #region Repeated Effect

    public class RepeatEffect : IEffectHolder
    {
        [Title("Total Time")]
        [OdinSerialize]
        public float ModifierValue { get; set; } = 0.5f;

        [Title("Effect")]
        [OdinSerialize]
        public List<IEffect> Effects { get; set; }

        [SerializeField]
        private float repeatFrequency = 0.1f;

        [SerializeField]
        private float initialDelay = 0.0f;

        public async void Perform(IAttacker unit)
        {
            float timer = repeatFrequency - initialDelay;
            float totalTimer = 0;

            while (totalTimer < ModifierValue)
            {
                await UniTask.Yield();

                totalTimer += Time.deltaTime;
                timer += Time.deltaTime;

                if (timer >= repeatFrequency)
                {
                    timer = 0;
                    for (int i = 0; i < Effects.Count; i++)
                    {
                        Effects[i].Perform(unit);
                    }
                }
            }
        }

        public void Revert(IAttacker unit)
        {
            for (int i = 0; i < Effects.Count; i++)
            {
                Effects[i].Revert(unit); // Might work
            }
        }

        public IEffectHolder Clone()
        {
            return new RepeatEffect
            {
                Effects = new List<IEffect>(),
                initialDelay = initialDelay,
                ModifierValue = ModifierValue,
                repeatFrequency = repeatFrequency
            };
        }
    }

    #endregion

    #region Status Effect

    public class StatusEffectEffect : IEffect
    {
        [Title("Duration")]
        [OdinSerialize]
        public float ModifierValue { get; set; } = 1;

        [InfoBox("Applies the effect to the user of the skill")]
        [SerializeField]
        private StatusEffect statusEffect;

        public async void Perform(IAttacker unit)
        {
            unit.Health.StatusEffects.Add(statusEffect);

            await UniTask.Delay(TimeSpan.FromSeconds(ModifierValue));

            unit.Health.StatusEffects.Remove(statusEffect);
        }

        public void Revert(IAttacker unit)
        {
            
        }
    }


    #endregion

    #region Targeted Effect

    public class OnDamageTargetedEffect : IEffectHolder
    {
        [Title("Nohting")]
        [OdinSerialize]
        public float ModifierValue { get; set; } = 10;

        [Title("Effect")]
        [OdinSerialize]
        public List<IEffect> Effects { get; set; }

        public IEffectHolder Clone()
        {
            return new OnDamageTargetedEffect()
            {
                Effects = new List<IEffect>(),
                ModifierValue = ModifierValue
            };
        }

        public void Perform(IAttacker attacker)
        {
            attacker.AttackPosition = attacker.LastDamageDone.TargetHit.Position;
            for (int i = 0; i < Effects.Count; i++)
            {
                Effects[i].Perform(attacker);
            }
        }

        public void Revert(IAttacker unit)
        {
            for (int i = 0; i < Effects.Count; i++)
            {
                Effects[i].Revert(unit); // Might work
            }
        }
    }

    #endregion

    #region Visual Effect

    public class VisualEffectEffect : IEffect 
    {
        [Title("Effect Scale")]
        [OdinSerialize]
        public float ModifierValue { get; set; } = 1;

        [Title("VFX")]
        [AssetSelector]
        public AttackVisualEffect AttackEffect;
        
        public void Perform(IAttacker unit)
        {
            Vector3 pos = unit.AttackPosition;

            AttackEffect.Spawn(pos, Quaternion.identity, ModifierValue);
        }

        public void Revert(IAttacker unit)
        {
            // Cant revert
        }
    }
    
    #endregion

    public enum EffectType
    {
        Effect,
        Holder,
        DoneDamage
    }
}
