using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.InputSystem;
using Gameplay.Event;
using InputCamera;
using System;

namespace Utility
{
    public class FocusManager : Singleton<FocusManager>
    {
        private HashSet<Focus> focuses = new HashSet<Focus>();
        
        private InputManager inputManager;

        private bool pressedGameobject;

        private void OnEnable()
        {
            GetInput().Forget();
        }

        private void OnDisable()
        {
            if (inputManager)
            {
                inputManager.Fire.started += FireStarted;
                inputManager.Fire.canceled += FireCanceled;
            }
        }

        private async UniTaskVoid GetInput()
        {
            inputManager = await InputManager.Get();
            inputManager.Fire.started += FireStarted;
            inputManager.Fire.canceled += FireCanceled;
        }

        private void FireStarted(InputAction.CallbackContext obj) // weo weo weo weo
        {
            pressedGameobject = !InputManager.MouseOverUI();
        }
        
        private void FireCanceled(InputAction.CallbackContext obj)
        {
            if (!pressedGameobject || InputManager.MouseOverUI()) return;
            if (CameraController.IsDragging) return;
            
            List<Focus> focusesToRemove = new List<Focus>();
            foreach (Focus focus in focuses)
            {
                if (focus.CloseConditions.HasFlag(FocusCloseCondition.GameobjectPress))
                {
                    focusesToRemove.Add(focus);
                }
            }

            foreach (Focus focus in focusesToRemove)
            {
                UnregisterFocus(focus);
            }
        }

        public void RegisterFocus(Focus focusToRegister)
        {
            UIEvents.OnFocusChanged?.Invoke();

            HashSet<Focus> toRemove = new HashSet<Focus>();
            foreach (Focus focus in focuses)
            {
                if (focus.ChangeType != FocusChangeType.Unique) continue;

                toRemove.Add(focus);
            }

            foreach (Focus focus in toRemove)
            {
                RemoveFocus(focus);
            }
            
            focuses.Add(focusToRegister);
        }

        public void UnregisterFocus(Focus focusToRemove)
        {
            RemoveFocus(focusToRemove);
        }
        
        private void RemoveFocus(Focus focus)
        {
            if (focuses.Remove(focus))
            {
                focus.OnFocusExit?.Invoke();
            }
        }
        
        public bool GetIsFocused(HashSet<FocusType> blackList = null)
        {
            if (blackList == null) return focuses.Count > 0;

            foreach (Focus focus in focuses)
            {
                if (!blackList.Contains(focus.FocusType))
                {
                    return true;
                }
            }

            return false;
        }
        
        public bool GetIsFocused(out HashSet<Focus> currentFocus)
        {
            currentFocus = focuses;
            return focuses.Count > 0;
        }
    }
    
    public class Focus
    {
        public FocusCloseCondition CloseConditions;
        public FocusChangeType ChangeType;
        public FocusType FocusType;
        public Action OnFocusExit;
    }

    public enum FocusType
    {
        Placing,
        DistrictPanel,
    }

    [Flags]
    public enum FocusCloseCondition
    {
        GameobjectPress = 1 << 0,
    }

    public enum FocusChangeType
    {
        Unique,
    }
}