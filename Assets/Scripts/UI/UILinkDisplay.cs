using UnityEngine.EventSystems;
using UnityEngine;
using System;
using TMPro;
using Variables;

namespace UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class UILinkDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public event Action<string> OnLinkEnter;
        public event Action<string> OnLinkExit;
     
        [SerializeField]
        private bool darkenTextOnHover;
        
        [SerializeField]
        private float tintingAlpha = 0.75f;
        
        private TextMeshProUGUI text;
        private Canvas canvas;
        private Camera cam;
        
        private bool isHovering;
        private int selectedLinkIndex = -1;

        private void Awake()
        {
            text = GetComponent<TextMeshProUGUI>();
            canvas = GetComponentInParent<Canvas>(); 
            
            cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }

        private void Update()
        {
            if (!isHovering) return;

            HandleLinkHover();
        }

        private void HandleLinkHover()
        {
            Vector3 mousePoint = Input.mousePosition;
            int linkTaggedText = TMP_TextUtilities.FindIntersectingLink(text, mousePoint, cam);

            if (linkTaggedText == -1)
            {
                if (selectedLinkIndex == -1) return;
                
                ExitLink(selectedLinkIndex);

                return;
            }
                
            if (linkTaggedText != selectedLinkIndex)
            {
                HoverLink(linkTaggedText);
            }
        }

        private void HoverLink(int linkTaggedText)
        {
            selectedLinkIndex = linkTaggedText;
            TMP_LinkInfo info = text.textInfo.linkInfo[linkTaggedText];

            if (darkenTextOnHover)
            {
                DarkenText(info, tintingAlpha);
            }
            
            OnLinkEnter?.Invoke(info.GetLinkID());
        }

        private void DarkenText(TMP_LinkInfo info, float tintingAlpha)
        {
            int firstCharacterIndex = info.linkTextfirstCharacterIndex;
            for (int characterIndex = firstCharacterIndex; characterIndex < firstCharacterIndex + info.linkTextLength; characterIndex++)
            {
                int meshIndex = text.textInfo.characterInfo[characterIndex].materialReferenceIndex;
                int vertexIndex = text.textInfo.characterInfo[characterIndex].vertexIndex;
                Color32[] vertexColors = text.textInfo.meshInfo[meshIndex].colors32;
                Color32 c = vertexColors[vertexIndex + 0].Tint(tintingAlpha);
                vertexColors[vertexIndex + 0] = c;
                vertexColors[vertexIndex + 1] = c;
                vertexColors[vertexIndex + 2] = c;
                vertexColors[vertexIndex + 3] = c;
            }

            // Update Geometry
            text.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
        }

        private void ExitLink(int linkTaggedText)
        {
            selectedLinkIndex = -1;
            TMP_LinkInfo info = text.textInfo.linkInfo[linkTaggedText];

            if (darkenTextOnHover)
            {
                DarkenText(info, 1.0f / tintingAlpha);
            }
            
            OnLinkExit?.Invoke(info.GetLinkID());                        
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovering = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;

            if (selectedLinkIndex != -1)
            {
                ExitLink(selectedLinkIndex);
            }
        }
    }
}