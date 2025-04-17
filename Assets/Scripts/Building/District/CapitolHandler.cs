using System.Collections.Generic;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;
using UnityEngine;
using System;
using Cysharp.Threading.Tasks;

namespace Buildings.District 
{
    public class CapitolHandler : MonoBehaviour
    {
        public static bool HasCapitol = false;
        
        [Title("Capitol")]
        [SerializeField]
        private CapitolDisplay capitolDisplayPrefab;
        
        [Title("References")]
        [SerializeField]
        private DistrictHandler districtHandler;
        
        private readonly List<CapitolDisplay> spawnedDisplays = new List<CapitolDisplay>();
        
        private bool displaying = false;
        
        private async void OnEnable()
        {
            UIEvents.OnCapitolClicked += OnCapitolClicked;
            Events.OnWaveStarted += OnWaveStarted;
            
            await UniTask.WaitUntil(() => InputManager.Instance != null);
            InputManager.Instance.Cancel.performed += OnCancel;
        }

        private void OnDisable()
        {
            InputManager.Instance.Cancel.performed -= OnCancel;
            UIEvents.OnCapitolClicked -= OnCapitolClicked;
            Events.OnWaveStarted -= OnWaveStarted;
        }
        
        private void OnWaveStarted()
        {
            if (displaying)
            {
                OnCancel(default);
            }
        }

        private void OnCapitolClicked()
        {
            if (displaying) return;
            
            foreach (DistrictData districtData in districtHandler.Districts)
            {
                if (districtData.State is not MineState mineState)
                {
                    continue;
                }

                Vector3 pos = districtData.Position + Vector3.up * 1.5f; 
                CapitolDisplay spawned = capitolDisplayPrefab.GetAtPosAndRot<CapitolDisplay>(pos, Quaternion.identity);
                spawned.CapitolHandler = this;
                spawned.Setup(mineState);
                spawnedDisplays.Add(spawned);
            }

            if (spawnedDisplays.Count > 0)
            {
                displaying = true;
            }
        }
        
        private void OnCancel(InputAction.CallbackContext obj)
        {
            displaying = false;
            foreach (var display in spawnedDisplays)
            {
                display.gameObject.SetActive(false);
            }
            
            spawnedDisplays.Clear();
        }

        public void ToggleIsCapitol(CapitolDisplay capitolDisplay)
        {
            bool value = !capitolDisplay.MineState.IsCapitol;
            capitolDisplay.MineState.IsCapitol = value;
            if (!value)
            {
                HasCapitol = false;
                return;
            }

            HasCapitol = true;
            foreach (CapitolDisplay display in spawnedDisplays)
            {
                if (display == capitolDisplay) continue;
                if (!display.MineState.IsCapitol) continue;

                display.MineState.IsCapitol = false;
                display.UpdateCapitolDisplay();
            }
        }
    }
}
