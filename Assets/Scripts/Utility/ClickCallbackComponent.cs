using System;
using UnityEngine;

namespace Utility
{
    public class ClickCallbackComponent : MonoBehaviour
    {
        public event Action OnClick;

        public void OnMouseDown()
        {
            OnClick?.Invoke();
        }
    }
}