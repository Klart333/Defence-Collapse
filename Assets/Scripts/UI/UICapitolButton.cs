using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UICapitolButton : MonoBehaviour
    {
        [SerializeField]
        private Button button;

        public void ShowCapitolPlacement()
        {
            UIEvents.OnCapitolClicked?.Invoke();
        }
    }
}