using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;
using UnityEngine;
using System;
using Loot;

namespace Buildings.District
{
    public class DistrictUpgradeManager : Singleton<DistrictUpgradeManager>
    {
        public event Action<EffectModifier> OnEffectGained;
        
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

        [SerializeField]
        private TowerData lightningData;

        [SerializeField]
        private TowerData churchData;

        [SerializeField]
        private TowerData barracksData;

        [Title("UI", "Upgrade")]
        [SerializeField]
        private UI.UIDistrictUpgrade districtUpgrade;

#if UNITY_EDITOR
        [Title("Debug")]
        [SerializeField]
        private List<LootData> debugStartingLootDatas;

        [SerializeField]
        private bool giveStartingLootData;
#endif

        private void OnEnable()
        {
            UIEvents.OnFocusChanged += Close;

#if UNITY_EDITOR
            if (giveStartingLootData)
            { 
                AddStartingLoot().Forget();
            }
#endif

            GetInput().Forget();
        }

#if UNITY_EDITOR
        private async UniTaskVoid AddStartingLoot()
        {
            await UniTask.Delay(500);
            foreach (LootData lot in debugStartingLootDatas)
            {
                lot.AddModifierEditorOnly();
            }
        }
#endif

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

            districtUpgrade.ShowUpgrades(district);
        }

        public void Close()
        {
            districtUpgrade.Close();
        }

        #region Modifier Effects

        public void AddModifierEffect(EffectModifier effect)
        {
            OnEffectGained?.Invoke(effect);
        }

        #endregion
    }
}