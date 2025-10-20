using System;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using UnityEngine;

namespace Juice.Cursor
{
    public class UICursorHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Title("Setup")]
        [SerializeField]
        private CursorTextureData hoverCursorData;
        
        [SerializeField]
        private CursorTextureData disabledTextureData;

        private Button button;
        private bool isHovered = false;

        private void Start()
        {
            button = GetComponentInChildren<Button>();
        }

        private void OnDisable()
        {
            if (!isHovered) return;
            
            CursorTextureData.SetCursorDefault();
            isHovered = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;

            if (button && !button.interactable)
            {
                CursorTextureData.SetCursorToData(disabledTextureData);
                return;
            }
            
            CursorTextureData.SetCursorToData(hoverCursorData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            CursorTextureData.SetCursorDefault();

            isHovered = false;
        }
    }
}