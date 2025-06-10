using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Buildings.District;
using UnityEngine.UI;
using Gameplay.Money;
using UnityEngine;
using DG.Tweening;
using InputCamera;
using System;
using TMPro;
using Loot;

namespace UI
{
    public class UIDistrictUpgrade : MonoBehaviour
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

        [SerializeField]
        private Image[] points;

        [Title("Upgrade Displays")]
        [SerializeField]
        private TextMeshProUGUI panelTitleText;

        [SerializeField]
        private UIUpgradeDisplay upgradeDisplayPrefab;

        [SerializeField]
        private Transform upgradeDisplayParent;

        [SerializeField]
        private UIFlexibleLayoutGroup flexibleLayoutGroup;

        [Title("Sections")]
        [SerializeField]
        private GameObject upgradeSection;

        [SerializeField]
        private GameObject modifySection;

        [SerializeField]
        private RectTransform line;

        [Title("Effect Panels")]
        [SerializeField]
        private float startX = -325;
        
        [SerializeField]
        private float endX = 325;
        
        [SerializeField]
        private UIEffectsHandler ownedEffectsPanel;

        [SerializeField]
        private UIEffectsHandler towerEffectsPanel;
        
        [Title("Stat Panel")]
        [SerializeField]
        private UIDistrictStatPanel statPanel;

        private readonly Dictionary<DistrictData, List<EffectModifier>> effectModifiers = new Dictionary<DistrictData, List<EffectModifier>>();

        private readonly List<EffectModifier> availableEffectModifiers = new List<EffectModifier>();
        private readonly List<UIUpgradeDisplay> spawnedDisplays = new List<UIUpgradeDisplay>();

        private DistrictUpgradeManager upgradeManager;
        private InputManager inputManager;
        private DistrictData districtData;
        private Canvas canvas;
        private Camera cam;

        private void OnEnable()
        {
            canvas = GetComponentInParent<Canvas>();
            cam = Camera.main;

            towerEffectsPanel.OnEffectRemoved += RemoveEffectFromTower;
            towerEffectsPanel.OnEffectAdded += AddEffectToTower;

            GetUpgradeManager().Forget();
            GetInput().Forget();
        }

        private async UniTaskVoid GetUpgradeManager()
        {
            upgradeManager = await DistrictUpgradeManager.Get();
            upgradeManager.OnEffectGained += OnEffectGained;
        }

        private async UniTaskVoid GetInput()
        {
            inputManager = await InputManager.Get();
            inputManager.Fire.canceled += ClickReleased;
        }

        private void OnDisable()
        {
            towerEffectsPanel.OnEffectRemoved -= RemoveEffectFromTower;
            towerEffectsPanel.OnEffectAdded -= AddEffectToTower;
            upgradeManager.OnEffectGained -= OnEffectGained;
            inputManager.Fire.canceled -= ClickReleased;
        }

        private void Update()
        {
            if (parentPanel.activeSelf)
            {
                PositionRectTransform.PositionOnOverlayCanvas(canvas, cam, parentPanel.transform as RectTransform, districtData.Position, pivot);
            }
        }

        private void ClickReleased(InputAction.CallbackContext obj)
        {
            if (parentPanel.activeSelf)
            {
                CheckCancel();
            }
        }

        private void CheckCancel()
        {
            if (InputManager.Instance.Fire.WasReleasedThisFrame() && !CameraController.IsDragging && !EventSystem.current.IsPointerOverGameObject())
            {
                UIEvents.OnFocusChanged?.Invoke();
            }
        }

        #region UI

        public void ShowSection(bool isUpgrade)
        {
            upgradeSection.SetActive(isUpgrade);
            modifySection.SetActive(!isUpgrade);

            line.DOKill();
            line.DOAnchorPosX(isUpgrade ? -100 : 100, 0.2f).SetEase(Ease.OutCirc);

            (ownedEffectsPanel.transform.parent as RectTransform).DOAnchorPosX(isUpgrade ? startX : endX, 0.5f).SetEase(Ease.OutCirc);

            if (!isUpgrade && effectModifiers.TryGetValue(districtData, out List<EffectModifier> effects))
            {
                towerEffectsPanel.SpawnEffects(effects);
            }
        }

        public void ShowUpgrades(DistrictData districtData)
        {
            this.districtData = districtData;

            parentPanel.SetActive(true);
            ownedEffectsPanel.SpawnEffects(availableEffectModifiers);

            (ownedEffectsPanel.transform.parent as RectTransform).anchoredPosition = Vector2.zero;

            ShowSection(true);

            SpawnUpgradeDisplays(districtData);
            panelTitleText.text = GetDistrictName(districtData);

            districtData.OnDisposed += DistrictDataOnOnDisposed;
            
            if (statPanel.gameObject.activeSelf)
            {
                statPanel.DisplayStats(StatDisplayableType.District, GetDistrictName(districtData), districtData.State.Stats);
            }

            void DistrictDataOnOnDisposed()
            {
                districtData.OnDisposed -= DistrictDataOnOnDisposed;

                if (effectModifiers.TryGetValue(districtData, out List<EffectModifier> districtEffects))
                {
                    availableEffectModifiers.AddRange(districtEffects);
                }

                effectModifiers.Remove(districtData);
                if (districtData == this.districtData)
                {
                    this.districtData = null;
                    parentPanel.SetActive(false);
                }
            }
        }

        private void SpawnUpgradeDisplays(DistrictData districtData)
        {
            for (int i = 0; i < districtData.UpgradeStats.Count; i++)
            {
                UIUpgradeDisplay spawned = upgradeDisplayPrefab.Get<UIUpgradeDisplay>();
                spawned.DistrictUpgrade = this;
                spawned.DisplayStat(districtData.UpgradeStats[i]);
                spawned.transform.SetParent(upgradeDisplayParent, false);
                spawned.transform.SetSiblingIndex(i);
                spawnedDisplays.Add(spawned);
            }

            flexibleLayoutGroup.coloumns = districtData.UpgradeStats.Count;
            flexibleLayoutGroup.CalculateNewBounds();

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
                    points[i].gameObject.SetActive(false);
                }
                else
                {
                    descriptions[i].text = string.Format(stat.Descriptions[i], i == 1 ? stat.Value.ToString("N") : stat.GetIncrease().ToString("N"));
                    points[i].gameObject.SetActive(true);
                }
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

        private void OnEffectGained(EffectModifier effectModifier)
        {
            availableEffectModifiers.Add(effectModifier);
            if (gameObject.activeInHierarchy)
            {
                Close();
            }
        }

        private void AddEffectToTower(EffectModifier effectModifier)
        {
            districtData.State.Attack.AddEffect(effectModifier.Effects, effectModifier.EffectType);

            availableEffectModifiers.Remove(effectModifier);

            if (effectModifiers.TryGetValue(districtData, out List<EffectModifier> list)) list.Add(effectModifier);
            else effectModifiers.Add(districtData, new List<EffectModifier> { effectModifier });
        }

        private void RemoveEffectFromTower(EffectModifier effectModifier)
        {
            districtData.State.Attack.RemoveEffect(effectModifier.Effects, effectModifier.EffectType);

            availableEffectModifiers.Add(effectModifier);

            if (!effectModifiers.TryGetValue(districtData, out List<EffectModifier> list)) return;
            list.Remove(effectModifier);
            if (list.Count == 0)
            {
                effectModifiers.Remove(districtData);
            }
        }

        #endregion

        #region Stats

        public void ToggleStatPanel()
        {
            statPanel.gameObject.SetActive(!statPanel.gameObject.activeSelf);
            if (statPanel.gameObject.activeSelf)
            {
                statPanel.DisplayStats(StatDisplayableType.District, GetDistrictName(districtData), districtData.State.Stats);
            }
        }

        #endregion
        
        private static string GetDistrictName(DistrictData districtData)
        {
            return districtData.State switch
            {
                TownHallState => "Town Hall",
                MineState => "Mine District",
                BombState => "Bomb District",
                ArcherState => "Archer District",
                FlameState => "Flame District",
                LightningState => "Lightning District",
                ChurchState => "Church District",

                _ => throw new ArgumentOutOfRangeException()
            };
        }

    }
}