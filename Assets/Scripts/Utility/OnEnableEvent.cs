using System;
using UnityEngine.Events;
using UnityEngine;

namespace Utility
{
    public class OnEnableEvent : MonoBehaviour
    {
        public UnityEvent EnableEvent;
        public UnityEvent DisableEvent;
        
        private void OnEnable()
        {
            EnableEvent.Invoke();
        }

        private void OnDisable()
        {
            DisableEvent.Invoke();
        }
    }
}