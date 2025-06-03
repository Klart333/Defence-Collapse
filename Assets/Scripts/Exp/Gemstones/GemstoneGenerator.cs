using System.Collections.Generic;
using NUnit.Framework;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Exp.Gemstones
{
    [CreateAssetMenu(fileName = "Gemstone Generator", menuName = "Exp/Gemstone/Gemstone Generator", order = 0)]
    public class GemstoneGenerator : SerializedScriptableObject
    {
        [Title("Level")]
        [SerializeField]
        private AnimationCurve effectCountCurve;
        
        [Title("Effects")]
        [SerializeField]
        private Dictionary<GemstoneType, IGemstoneBakingEffect[]> bakingEffects = new Dictionary<GemstoneType, IGemstoneBakingEffect[]>();
        
        public Gemstone GetGemstone(GemstoneType gemstoneType, int level)
        {
            int effectCount = (int)effectCountCurve.Evaluate(level);
            List<IGemstoneEffect> effects = new List<IGemstoneEffect>();
            IGemstoneBakingEffect[] possibleEffects = bakingEffects[gemstoneType];
            for (int i = 0; i < effectCount; i++)
            {
                int index = UnityEngine.Random.Range(0, possibleEffects.Length);
                IGemstoneEffect effect = possibleEffects[index].GetEffect(level);
                effects.Add(effect);
            }
            
            Gemstone gemstone = new Gemstone
            {
                GemstoneType = gemstoneType,
                Level = level,
                Effects = effects.ToArray()
            };
            
            return gemstone;
        }

#if UNITY_EDITOR
        [Button]
        private void Save()
        {
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
        }
#endif
    }
}