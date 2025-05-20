using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Effects;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;
using UnityEngine;
using Loot;

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
        private TowerData flameData;

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

#if UNITY_EDITOR
        [Title("Debug")]
        [SerializeField]
        private List<LootData> debugStartingLootDatas;

        [SerializeField]
        private bool giveStartingLootData;
#endif

        private readonly List<EffectModifier> modifierEffectsToSpawn = new List<EffectModifier>();

        public List<EffectModifier> ModifierEffects => modifierEffectsToSpawn;
        public TowerData ArcherData => archerData;
        public TowerData BombData => bombData;
        public TowerData FlameData => flameData;
        public TowerData MineData => mineData;
        public TowerData TownHallData => townHallData;

        private void OnEnable()
        {
            UIEvents.OnFocusChanged += Close;

#if UNITY_EDITOR
            if (giveStartingLootData)
            {
                foreach (LootData lot in debugStartingLootDatas)
                {
                    lot.AddModifierEditorOnly();
                }   
            }
#endif

            GetInput().Forget();
        }

        private async UniTaskVoid GetInput()
        {
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

        public async UniTaskVoid OpenUpgradeMenu(DistrictData district)
        {
            UIEvents.OnFocusChanged?.Invoke();
            await UniTask.Yield();

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