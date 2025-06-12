using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using System;
using UnityEditor;

namespace Effects
{
    public interface IStat
    {
        public event Action OnValueChanged;
        public float Value { get; }
        public float BaseValue { get; }

        public void AddModifier(Modifier mod);
        public void RemoveModifier(Modifier mod);
        public void RemoveAllModifiers();
    }

    [Serializable, InlineProperty]
    public class Stat : IStat
    {
        public event Action OnValueChanged;

        public float BaseValue
        {
            get => baseValue;
            set
            {
                if (Mathf.Approximately(baseValue, value)) return;

                baseValue = value;
                isDirty = true;
                OnValueChanged?.Invoke();
            }
        }

        [SerializeField]
        private float baseValue;

        private HashSet<Modifier> modifiers = new HashSet<Modifier>();
        private Modifier percentModifier = null;

        private float value;
        private bool isDirty = true;

        private readonly Modifier.ModifierType[] ModifierTypes =
        {
            Modifier.ModifierType.Additive,
            Modifier.ModifierType.Multiplicative,
            Modifier.ModifierType.PercentCumulative,
        };

        [ShowInInspector, ReadOnly]
        public float Value
        {
            get
            {
#if UNITY_EDITOR
                if (!EditorApplication.isPlaying)
                {
                    isDirty = true;
                    value = GetValue();
                    return value;
                }
#endif

                if (!isDirty)
                {
                    return value;
                }

                value = GetValue();
                isDirty = false;

                return value;
            }
        }

        public float GetValue()
        {
            float val = BaseValue;
            if (modifiers == null)
            {
                return val;
            }

            foreach (Modifier.ModifierType modifierType in ModifierTypes)
            {
                foreach (Modifier modifier in modifiers)
                {
                    if (modifier.Type != modifierType)
                    {
                        continue;
                    }

                    val = modifier.Type switch
                    {
                        Modifier.ModifierType.Multiplicative => val * modifier.Value,
                        Modifier.ModifierType.Additive => val + modifier.Value,
                        Modifier.ModifierType.PercentCumulative => val + val * modifier.Value,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                }
            }

            return val;
        }

        public Stat(float baseValue)
        {
            BaseValue = baseValue;
        }

        public void AddModifier(Modifier mod)
        {
            if (mod.Type == Modifier.ModifierType.PercentCumulative && percentModifier != null)
            {
                percentModifier.Value += mod.Value;
            }
            else
            {
                if (mod.Type == Modifier.ModifierType.PercentCumulative)
                {
                    mod = new Modifier(mod);
                    percentModifier = mod;
                }
                modifiers.Add(mod);

            }

            isDirty = true;
            OnValueChanged?.Invoke();
        }

        public void RemoveModifier(Modifier mod)
        {
            if (mod.Type == Modifier.ModifierType.PercentCumulative && percentModifier != null)
            {
                percentModifier.Value -= mod.Value;
            }
            else
            {
                if (!modifiers.Remove(mod))
                {
                    Debug.LogError("Could not find modifier to remove");
                    return;
                }   
            }

            isDirty = true;
            OnValueChanged?.Invoke();
        }

        public void RemoveAllModifiers()
        {
            if (modifiers.Count == 0)
            {
                return;
            }
            
            modifiers.Clear();

            isDirty = true;
            OnValueChanged?.Invoke();
        }

        public void SetDirty(bool silent = true)
        {
            isDirty = true;

            if (!silent)
            {
                OnValueChanged?.Invoke();
            }
        }

        public static implicit operator float(Stat stat) => stat.Value;

        public override string ToString()
        {
            return $"BaseValue: {BaseValue:N}, Value: {Value:N}";
        }
    }

    [Serializable]
    public class Modifier
    {
        public enum ModifierType
        {
            Additive = 0,
            Multiplicative = 1,
            PercentCumulative = 2,
        }

        public ModifierType Type;

        public float Value;


        public Modifier() { }

        public Modifier(Modifier copy)
        {
            Type = copy.Type;
            Value = copy.Value;
        }
    }
}