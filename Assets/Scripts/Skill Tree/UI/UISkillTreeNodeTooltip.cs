using System.Collections.Generic;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using UnityEngine;
using UI;

namespace SkillTree.UI
{
    [RequireComponent(typeof(UISkillTreeNode))]
    public class UISkillTreeNodeTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Title("Tooltip")]
        [SerializeField]
        private float heightOffset = 10;
        
        [SerializeField]
        private bool tooltipBlocksRaycasts;
     
        [Title("Tooltip", "Font size")]
        [SerializeField]
        private float titleFontSize = 30;
        
        [SerializeField]
        private float descriptionFontSize = 20;
        
        private UITooltipHandler tooltipHandler;
        private RectTransform rectTransform;
        private UISkillTreeNode node;
        private Canvas canvas;
        
        private void Awake()
        {
            tooltipHandler = FindFirstObjectByType<UITooltipHandler>();
            rectTransform = transform as RectTransform;
            canvas = GetComponentInParent<Canvas>();
            node = GetComponent<UISkillTreeNode>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            List<TextData> skillDescription = new List<TextData>
            {
                new TextData(node.SkillNodeDescription.Title, titleFontSize),
                new TextData(node.SkillNodeDescription.Description, descriptionFontSize),
            };

            Vector2 position = ToolTipUtility.GetTooltipPosition(rectTransform, canvas, heightOffset);
            tooltipHandler.DisplayTooltip(skillDescription, position, tooltipBlocksRaycasts);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            tooltipHandler.PointerExitPanel();
        }

    }
}