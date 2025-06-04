using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Text;
using UnityEngine;
using Effects;
using TMPro;

namespace Exp.Gemstones
{
    public class UIActiveGemstoneEffectDisplay : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI bonusText;

        [SerializeField]
        private StatNameUtility statNameUtility;
        
        private ExpManager expManager;
        
        private void OnEnable()
        {
            GetExpManager().Forget();
        }

        private void OnDisable()
        {
            expManager.OnActiveGemstonesChanged -= UpdateText;
        }

        private async UniTaskVoid GetExpManager()
        {
            expManager = await ExpManager.Get();
            
            expManager.OnActiveGemstonesChanged += UpdateText;
            UpdateText();
        }

        private void UpdateText()
        {
            StringBuilder stringBuilder = new StringBuilder();
            Dictionary<StatType, float> stats = new Dictionary<StatType, float>();
            foreach (Gemstone gemstone in expManager.ActiveGemstones)
            {
                for (int i = 0; i < gemstone.Effects.Length; i++)
                {
                    if (gemstone.Effects[i] is StatIncreaseEffect { Effect: IncreaseStatEffect stat })
                    {
                        bool exists = stats.TryGetValue(stat.StatType, out float value);
                        value += stat.ModifierValue;

                        if (exists)
                        {
                            stats[stat.StatType] = value;
                        }
                        else
                        {
                            stats.Add(stat.StatType, value);
                        }
                        
                        continue;
                    }
                    
                    stringBuilder.AppendLine(gemstone.Effects[i].GetDescription());
                }
            }

            foreach (KeyValuePair<StatType, float> stat in stats)
            {
                stringBuilder.AppendLine(statNameUtility.GetDescription(stat.Key, stat.Value));
            }
            
            bonusText.text = stringBuilder.ToString();
        }
    }
}