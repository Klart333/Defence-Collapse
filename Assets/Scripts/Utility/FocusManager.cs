using System.Collections.Generic;
using System;
using Gameplay.Event;

namespace Utility
{
    public class FocusManager : Singleton<FocusManager>
    {
        private List<Focus> focuses = new List<Focus>();

        public void RegisterFocus(Focus focusToRegister)
        {
            UIEvents.OnFocusChanged?.Invoke();

            for (int i = focuses.Count - 1; i >= 0; i--)
            {
                if (focuses[i].ChangeType != FocusChangeType.Unique) continue;
                
                focuses[i].OnFocusExit();
                focuses.RemoveAt(i);
            }
            
            focuses.Add(focusToRegister);
        }

        public void UnregisterFocus(Focus focusToRemove)
        {
            for (int i = 0; i < focuses.Count; i++)
            {
                Focus focus = focuses[i];
                if (focusToRemove != focus) continue;
                
                focuses.RemoveAt(i);
                focus.OnFocusExit();
                break;
            }
        }

        public bool GetIsFocused(HashSet<FocusType> blackList = null)
        {
            if (blackList == null) return focuses.Count > 0;
            
            for (int i = 0; i < focuses.Count; i++)
            {
                if (!blackList.Contains(focuses[i].FocusType))
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