using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Utility;
using System;

namespace UI
{
    [RequireComponent(typeof(UILinkDisplay))]
    public class UILinkTooltipDisplay : MonoBehaviour
    {
        [Title("References")]
        [SerializeField]
        private LinkIdUtility linkIdUtility;

        [Title("Tooltip")]
        [SerializeField]
        private float heightOffset = 10;
        
        [SerializeField]
        private bool useParentRectTransform = true;
        
        private RectTransform rectTransform;
        private UILinkDisplay linkDisplay;
        private UITooltipHandler handler;
        private Canvas canvas;

        private void Awake()
        {
            rectTransform = useParentRectTransform ? transform.parent as RectTransform : GetComponent<RectTransform>();
            
            handler = FindFirstObjectByType<UITooltipHandler>();
            linkDisplay = GetComponent<UILinkDisplay>();
            canvas = GetComponentInParent<Canvas>(); 
        }

        private void OnEnable()
        {
            linkDisplay.OnLinkEnter += OnLinkEnter;
            linkDisplay.OnLinkExit += OnLinkExit;
        }

        private void OnDisable()
        {
            linkDisplay.OnLinkEnter -= OnLinkEnter;
            linkDisplay.OnLinkExit -= OnLinkExit;
        }

        private void OnLinkEnter(string id)
        {
            LinkTooltip value = linkIdUtility.GetLinkId(id);
            List<Tuple<string, int>> description = new List<Tuple<string, int>>();
            for (int i = 0; i < value.Texts.Length; i++)
            {
                description.Add(new Tuple<string, int>(value.Texts[i].Value, value.TextSizes[i]));
            }

            Vector2 position = ToolTipUtility.GetTooltipPosition(rectTransform, canvas, heightOffset);
            handler.DisplayTooltip(description, position);
        }

        private void OnLinkExit(string id)
        {
            handler.PointerExitPanel();
        }
    }
}