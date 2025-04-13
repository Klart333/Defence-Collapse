using System;
using UnityEngine;

namespace Utility
{
    public class ClickCallbackComponent : MonoBehaviour
    {
        public event Action OnClick;
        
        private void OnMouseUpAsButton()
        {
            OnClick?.Invoke();
        }
    }
}