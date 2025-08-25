using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Variables;

namespace Utility
{
    [CreateAssetMenu(fileName = "Link ID Utlity", menuName = "Utility/Link Id Utility", order = 0)]
    public class LinkIdUtility : SerializedScriptableObject
    {
        [SerializeField]
        private List<LinkTooltipEditor> linkIds = new List<LinkTooltipEditor>();

        private Dictionary<string, LinkTooltip> linkTooltips;

#if UNITY_EDITOR
        private void OnValidate()
        {
            linkTooltips = null;
        }
#endif

        public LinkTooltip GetLinkId(string linkId)
        {
            linkTooltips ??= CreateDictionary();
            
            if (linkTooltips.TryGetValue(linkId, out LinkTooltip result))
            {
                return result;
            }
            
            Debug.LogError("ID: " + linkId + " not found");
            return new LinkTooltip();
        }

        private Dictionary<string, LinkTooltip> CreateDictionary()
        {
            var dict = new Dictionary<string, LinkTooltip>();
            foreach (LinkTooltipEditor value in linkIds)
            {
                dict.Add(value.Key.Value, value.Tooltip);
            }
            
            return dict;
        }
    }
    
    [System.Serializable]
    public class LinkTooltipEditor
    {
        public StringReference Key;
        public LinkTooltip Tooltip;
    }

    [System.Serializable]
    public class LinkTooltip
    {
        public StringReference[] Texts = new StringReference[1];
        public int[] TextSizes = new int[]{10};
    }
}