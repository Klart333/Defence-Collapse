using System;
using UnityEngine;

namespace Utility
{
    public class ClickCallbackComponent : MonoBehaviour
    {
        public event Action OnClick;
        public event Action OnHoverEnter;
        public event Action OnHoverExit;

        private void OnMouseEnter()
        {
            OnHoverEnter?.Invoke();
        }

        private void OnMouseExit()
        {
            OnHoverExit?.Invoke();
        }

        private void OnMouseUpAsButton()
        {
            OnClick?.Invoke();
        }
    }
}