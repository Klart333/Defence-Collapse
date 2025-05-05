using System.Collections.Generic;
using Sirenix.OdinInspector;
using Buildings.District;
using UnityEngine;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Loot;
using UnityEngine.InputSystem;

namespace Buildings.District
{
    public class DistrictUpgradeManager : Singleton<DistrictUpgradeManager>
    {
        [Title("State Data")]
        [SerializeField]
        private TowerData archerData;

        [SerializeField]
        private TowerData bombData;

        [SerializeField]
        private TowerData mineData;

        [SerializeField]
        private TowerData townHallData;

        [Title("UI")]
        [SerializeField]
        private GameObject canvas;

        [Title("UI", "Upgrade")]
        [SerializeField]
        private UIDistrictUpgrade districtUpgrade;

        private readonly List<EffectModifier> modifierEffectsToSpawn = new List<EffectModifier>();

        public List<EffectModifier> ModifierEffects => modifierEffectsToSpawn;
        public TowerData ArcherData => archerData;
        public TowerData BombData => bombData;
        public TowerData MineData => mineData;
        public TowerData TownHallData => townHallData;

        private async void OnEnable()
        {
            UIEvents.OnFocusChanged += Close;

            await UniTask.WaitUntil(() => InputManager.Instance != null);
            InputManager.Instance.Cancel.performed += Cancel_performed;
        }

        private void OnDisable()
        {
            InputManager.Instance.Cancel.performed -= Cancel_performed;
            UIEvents.OnFocusChanged -= Close;
        }

        private void Cancel_performed(InputAction.CallbackContext obj)
        {
            Close();
        }

        public async void OpenUpgradeMenu(DistrictData district)
        {
            UIEvents.OnFocusChanged?.Invoke();
            await Task.Yield();

            canvas.SetActive(true);
            districtUpgrade.ShowUpgrades(district);
        }

        public void Close()
        {
            if (!canvas.activeSelf) return;

            canvas.SetActive(false);
            districtUpgrade.Close();
        }

        #region Modifier Effects

        public void AddModifierEffect(EffectModifier effect)
        {
            modifierEffectsToSpawn.Add(effect);
        }

        #endregion
    }
}