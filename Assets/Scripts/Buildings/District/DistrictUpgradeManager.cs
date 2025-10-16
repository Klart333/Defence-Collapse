using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;
using Buildings.District.UI;
using Gameplay.Event;
using InputCamera;
using UnityEngine;
using Loot;
using Utility;
using FocusType = Utility.FocusType;

namespace Buildings.District
{
    public class DistrictUpgradeManager : Singleton<DistrictUpgradeManager>
    {
        [Title("UI", "Upgrade")]
        [SerializeField]
        private UIDistrictUpgrade districtUpgrade;

        private InputManager inputManager;
        private FocusManager focusManager;
        private Focus focus;
        
#if UNITY_EDITOR
        [Title("Debug")]
        [SerializeField]
        private List<LootData> debugStartingLootDatas;

        [SerializeField]
        private bool giveStartingLootData;
#endif
        
        private void OnEnable()
        {
            focus = new Focus
            {
                CloseConditions = FocusCloseCondition.GameobjectPress,
                ChangeType = FocusChangeType.Unique,
                FocusType = FocusType.DistrictPanel,
                OnFocusExit = Close,
            };
            
#if UNITY_EDITOR
            if (giveStartingLootData)
            { 
                AddStartingLoot().Forget();
            }
#endif

            GetInput().Forget();
            GetFocus().Forget();
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
            inputManager = await InputManager.Get();
            inputManager.Cancel.performed += CancelPerformed;
        }
        
        private async UniTaskVoid GetFocus()
        {
            focusManager = await FocusManager.Get();
        }

        private void OnDisable()
        {
            if (inputManager)
            {
                inputManager.Cancel.performed -= CancelPerformed;
            }
        }

        private void CancelPerformed(InputAction.CallbackContext obj)
        {
            focusManager.UnregisterFocus(focus);
        }

        public void OpenUpgradeMenu(DistrictData district)
        {
            focusManager.RegisterFocus(focus);
            district.RegisterToDistrictPanelFocus(focus);
            districtUpgrade.OpenDistrictPanel(district);
        }

        public void Close()
        {
            districtUpgrade.Close();
        }

        public void AddModifierEffect(EffectModifier effect)
        {
            Events.OnEffectGained?.Invoke(effect);
        }
    }
}