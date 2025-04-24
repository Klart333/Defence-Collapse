using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay
{
    [InlineEditor, CreateAssetMenu(fileName = "New Level Data", menuName = "Building/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Title("Cost")]
        [SerializeField]
        private EquationData costData;
        
        [Title("Increase")]
        [SerializeField]
        private EquationData increaseData;
        
        public float GetCost(int level)
        {
            return costData.Calculate(level);
        }

        public float GetIncrease(int level)
        {
            return increaseData.Calculate(level);
        }
    }

    [System.Serializable]
    public struct EquationData
    {
        [Title("Math")]
        [SerializeField]
        private EquationType equationType;

        [SerializeField]
        private float baseValue;
        
        [SerializeField]
        private float multiplier;

        [HideIf(nameof(equationType), EquationType.Linear)]
        [SerializeField]
        private float powerMultiplier;
        
        [ShowIf(nameof(equationType),EquationType.InverseExponential)]
        [SerializeField]
        private float baseExponent;
        
        [ShowIf(nameof(equationType),EquationType.Exponential)]
        [SerializeField]
        private float power;
        
        
        public float Calculate(int level)
        {
            return equationType switch
            {
                EquationType.Linear => baseValue + level * multiplier,
                EquationType.Exponential => baseValue + level * multiplier + Mathf.Pow(level, power) * powerMultiplier,
                EquationType.InverseExponential => baseValue + level * multiplier + Mathf.Pow(baseExponent, level) * powerMultiplier,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

#if UNITY_EDITOR
        [Title("Debug")]
        [SerializeField]
        private AnimationCurve debugDisplayCurve;

        [Button]
        private void DebugDisplay(int upper)
        {
            debugDisplayCurve.ClearKeys();
            for (int i = 0; i < upper; i++)
            {
                debugDisplayCurve.AddKey(new Keyframe(i, Calculate(i)));
            }

            for (int i = 0; i < debugDisplayCurve.keys.Length; ++i) 
            {
                debugDisplayCurve.SmoothTangents(i, 0); 
            }
        }
#endif
    }

    public enum EquationType
    {
        Linear,
        Exponential,
        InverseExponential,
    }
}