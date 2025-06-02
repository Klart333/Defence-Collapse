using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Gameplay.GameOver
{
    [CreateAssetMenu(fileName = "Name_ExpFactor", menuName = "Exp/Exp Factor", order = 0)]
    public class ExpFactor : SerializedScriptableObject
    {
        [Title("Exp")]
        [SerializeField]
        private AnimationCurve expFactor;

        [SerializeField, TextArea]
        private string displayText = "10 Waves - {0}";

        [SerializeField]
        private FactorType factorType;
        
        [OdinSerialize]
        private IGetFactor factor;

        public FactorType FactorType => factorType;
        public string DisplayText => displayText;
        
        public float GetFactor(out float level) => factor.GetFactor(expFactor, out level);
        
        public string GetDisplayLevel(float level) => factor.GetDisplayText(level);
    }

    public enum FactorType
    {
        Exp,
        Multiplier,
    }
}