using System.Collections.Generic;
using Gameplay.Event;
using System;

namespace Utility
{
    public class FocusManager : Singleton<FocusManager>
    {
        private HashSet<Focus> focuses = new HashSet<Focus>();

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
    }
    
    public class Focus
    {
        public FocusType FocusType;
        public FocusChangeType ChangeType;
        public Action OnFocusExit;
    }

    public enum FocusType
    {
        Placing,
    }

    public enum FocusChangeType
    {
        Unique,
    }
}