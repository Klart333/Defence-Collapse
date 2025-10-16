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

        private void Start()
        {
            button = GetComponentInChildren<Button>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
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
        }
    }
}