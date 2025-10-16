using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Gameplay.Money;
using Gameplay.Event;
using UnityEngine;
using DG.Tweening;
using InputCamera;
using Effects.UI;
using Effects;
using TMPro;
using Loot;
using UI;
using UnityEngine.Serialization;

namespace Buildings.District.UI
{
    public class UIDistrictUpgrade : MonoBehaviour // Todo: Convert to smaller scripts plz
    {
        [Title("Panel")]
        [SerializeField]
        private GameObject parentPanel;

        [SerializeField]
        private Vector2 pivot = new Vector2(0.5f, 0.5f);

        [Title("Upgrade")]
        [SerializeField]
        private TextMeshProUGUI upgradeTitleText;

        [SerializeField]
        private TextMeshProUGUI costText;

        [SerializeField]
        private TextMeshProUGUI[] descriptions;
        
        [Title("Upgrade Displays")]
        [SerializeField]
        private TextMeshProUGUI panelTitleText;

        [SerializeField]
        private UIUpgradeDisplay upgradeDisplayPrefab;

        [SerializeField]
        private Transform upgradeDisplayParent;

        [Title("Sections")]
        [SerializeField]
        private GameObject[] sections;

        [SerializeField]
        private float[] selectorSpacingOffsets;
        
        [SerializeField]
        private RectTransform line;

        [Title("Effect Panels")]
        [SerializeField]
        private UIEffectContainer towerEffectsPanel;

        [FormerlySerializedAs("containerAmount")]
        [SerializeField]
        private int effectContainerAmount = 3;
        
        [Title("Information")]
        [SerializeField]
        private UIAttachmentDisplayHandler attachmentHandler;
        
        [Title("Extra Info")]
        [SerializeField]
        private Transform extraInfoParent;
        
        [SerializeField]
        private PooledText extraInfoTextPrefab;
        
        [Title("Stat Panel")]
        [SerializeField]
        private UIDistrictStatPanel statPanel;

        private readonly Dictionary<DistrictData, EffectModifier[]> appliedEffectModifiers = new Dictionary<DistrictData, EffectModifier[]>();

        private readonly List<UIUpgradeDisplay> spawnedDisplays = new List<UIUpgradeDisplay>();
        private readonly List<PooledText> spawnedTexts = new List<PooledText>();

        private DistrictUpgradeManager upgradeManager;
        private DistrictData districtData;
        
        private void OnEnable()
        {
            towerEffectsPanel.OnEffectRemoved += RemoveEffectFromTower;
            towerEffectsPanel.OnEffectAdded += AddEffectToTower;
        }

        private void OnDisable()
        {
            towerEffectsPanel.OnEffectRemoved -= RemoveEffectFromTower;
            towerEffectsPanel.OnEffectAdded -= AddEffectToTower;
        }

        #region UI
        
        public void OpenDistrictPanel(DistrictData districtData)
        {
            this.districtData = districtData;
            if (districtData.State is IAttackerStatistics stats)
            {
                stats.OnStatisticsChanged += OnStatisticsChanged;
            }

            parentPanel.SetActive(true);
            
            SetupEffects(districtData);
            
            SpawnUpgradeDisplays(districtData);
            DisplayExtraInfo(districtData);
            panelTitleText.text = districtData.TowerData.DistrictName;

            districtData.OnDisposed += DistrictDataOnOnDisposed;
            
            if (statPanel.gameObject.activeSelf)
            {
                statPanel.DisplayStats(StatDisplayableType.District, districtData.TowerData.DistrictName, districtData.State.Stats);
            }

            void DistrictDataOnOnDisposed()
            {
                districtData.OnDisposed -= DistrictDataOnOnDisposed;

                if (appliedEffectModifiers.TryGetValue(districtData, out EffectModifier[] districtEffects))
                {
                    foreach (EffectModifier effect in districtEffects)
                    {
                        if (effect != null)
                        {
                            Events.OnEffectGained?.Invoke(effect);
                        }
                    }
                }

                appliedEffectModifiers.Remove(districtData);
                if (districtData == this.districtData)
                {
                    this.districtData = null;
                    parentPanel.SetActive(false);
                }
            }
        }

        private void SetupEffects(DistrictData districtData)
        {
            towerEffectsPanel.Setup(effectContainerAmount);
            
            if (appliedEffectModifiers.TryGetValue(districtData, out EffectModifier[] effects))
            {
                towerEffectsPanel.SetEffects(effects);
            }
        }

        private void DisplayExtraInfo(DistrictData districtData) // TODO: Convert to LocalizedString
        {
            bool spawned = false;

            if (districtData.State is IAttackerStatistics stats)
            {
                if (stats.DamageDone > 0)
                {
                    AddText($"Damage Dealt - {stats.DamageDone:N0}");
                }

                if (stats.GoldGained > 0)
                {
                    AddText($"Gold Gained - {stats.GoldGained:N0}");
                }
            }

            if (districtData.State is ILumbermillStatistics lumbermillStatistics)
            {
                AddText($"Forest chopped in <u>{lumbermillStatistics.TurnsUntilComplete}</u> Turns!");
            }
            
            if (spawned)
            {
                extraInfoParent.gameObject.SetActive(true);
            }

            void AddText(string textContent)
            {
                spawned = true;

                PooledText text = extraInfoTextPrefab.GetDisabled<PooledText>();
                text.transform.SetParent(extraInfoParent, false);
                text.Text.text = textContent;
                text.gameObject.SetActive(true);
                spawnedTexts.Add(text);
            }
        }

        private void OnStatisticsChanged()
        {
            for (int i = 0; i < spawnedTexts.Count; i++)
            {
                spawnedTexts[i].gameObject.SetActive(false);
            }
            spawnedTexts.Clear();
            DisplayExtraInfo(districtData);
        }

        private void SpawnUpgradeDisplays(DistrictData districtData)
        {
            if (districtData.UpgradeStats.Count <= 0)
            {
                return;
            }

            for (int i = 0; i < districtData.UpgradeStats.Count; i++)
            {
                UIUpgradeDisplay spawned = upgradeDisplayPrefab.Get<UIUpgradeDisplay>();
                spawned.DistrictUpgrade = this;
                spawned.DisplayStat(districtData.UpgradeStats[i]);
                spawned.transform.SetParent(upgradeDisplayParent, false);
                spawned.transform.SetSiblingIndex(i);
                spawnedDisplays.Add(spawned);
            }

            DisplayUpgrade(districtData.UpgradeStats[0]);
        }

        public void DisplayUpgrade(IUpgradeStat stat)
        {
            upgradeTitleText.text = stat.Name;

            for (int i = 0; i < descriptions.Length; i++)
            {
                if (i >= stat.Descriptions.Length)
                {
                    descriptions[i].text = "";
                    continue;
                }
                
                descriptions[i].text = string.Format(stat.Descriptions[i], i == 1 
                    ? "stat.Value.ToString(\"N\")" 
                    : stat.GetIncrease().ToString(stat.GetFormat()));
            }

            costText.text = $"Cost: {stat.GetCost()}";
        }

        public void Close()
        {
            if (!parentPanel.activeSelf)
            {
                return;
            }

            for (int i = 0; i < spawnedDisplays.Count; i++)
            {
                spawnedDisplays[i].gameObject.SetActive(false);
            }

            for (int i = 0; i < spawnedTexts.Count; i++)
            {
                spawnedTexts[i].gameObject.SetActive(false);
            }
            extraInfoParent.gameObject.SetActive(false);
            
            if (districtData.State is IAttackerStatistics stats)
            {
                stats.OnStatisticsChanged -= OnStatisticsChanged;
            }
            
            parentPanel.SetActive(false);
        }

        #endregion

        #region Upgrades

        public void UpgradeStat(IUpgradeStat stat)
        {
            float cost = stat.GetCost();
            stat.IncreaseLevel();

            MoneyManager.Instance.RemoveMoney(cost);

            districtData.LevelUp();

            DisplayUpgrade(stat);
        }

        public bool CanPurchase(IUpgradeStat upgradeStat)
        {
            return MoneyManager.Instance.Money >= upgradeStat.GetCost();
        }

        #endregion

        #region Effects

        private void AddEffectToTower(EffectModifier effectModifier, int containerIndex)
        {
            if (appliedEffectModifiers.TryGetValue(districtData, out EffectModifier[] effects))
            {
                if (effects[containerIndex] == effectModifier)
                {
                    return;
                }
                
                effects[containerIndex] = effectModifier;
            }
            else
            {
                EffectModifier[] appliedEffects = new EffectModifier[effectContainerAmount];
                appliedEffects[containerIndex] = effectModifier;
                appliedEffectModifiers.Add(districtData, appliedEffects);
            }

            districtData.State.Attack.AddEffect(effectModifier.Effects, effectModifier.EffectType);
        }

        private void RemoveEffectFromTower(EffectModifier effectModifier, int containerIndex)
        {
            districtData.State.Attack.RemoveEffect(effectModifier.Effects, effectModifier.EffectType);
            
            appliedEffectModifiers[districtData][containerIndex] = null;
        }

        #endregion

        #region Stats

        public void ToggleStatPanel()
        {
            statPanel.gameObject.SetActive(!statPanel.gameObject.activeSelf);
            if (statPanel.gameObject.activeSelf)
            {
                statPanel.DisplayStats(StatDisplayableType.District, districtData.TowerData.DistrictName, districtData.State.Stats);
            }
        }

        #endregion
    }
}