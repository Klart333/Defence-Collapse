using UnityEngine.Events;
using UnityEngine;

namespace Utility
{
    public class OnEnableEvent : MonoBehaviour
    {
        public UnityEvent EnableEvent;
        
        private void OnEnable()
        {
            EnableEvent.Invoke();
        }
    }
}