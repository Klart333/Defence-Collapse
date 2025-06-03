using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;

namespace UI
{
    public class UITooltipHandler : MonoBehaviour
    {
        [SerializeField]
        private Transform tooltipPanel;
        
        [SerializeField]
        private Transform contentParent;
        
        [SerializeField]
        private TextMeshProUGUI tooltipTextPrefab;
        
        private Queue<TextMeshProUGUI> textPool = new Queue<TextMeshProUGUI>();
        private List<TextMeshProUGUI> spawnedTexts = new List<TextMeshProUGUI>();
        
        public void DisplayTooltip(IEnumerable<Tuple<string, int>> tooltips, Vector2 position)
        {
            (tooltipPanel as RectTransform).anchoredPosition = position;
            
            tooltipPanel.gameObject.SetActive(true);
            foreach (Tuple<string, int> tooltip in tooltips)
            {
                TextMeshProUGUI text = GetText();
                text.transform.SetAsLastSibling();
                text.text = tooltip.Item1;
                text.fontSize = tooltip.Item2;
            }
        }

        private TextMeshProUGUI GetText()
        {
            if (textPool.TryDequeue(out TextMeshProUGUI text))
            {
                text.gameObject.SetActive(true);
                spawnedTexts.Add(text);
                return text;
            }
            
            text = Instantiate(tooltipTextPrefab, contentParent);
            spawnedTexts.Add(text);
            return text;
        }

        public void HideTooltip()
        {
            tooltipPanel.gameObject.SetActive(false);
            for (int i = 0; i < spawnedTexts.Count; i++)
            {
                spawnedTexts[i].gameObject.SetActive(false);
                textPool.Enqueue(spawnedTexts[i]);
            }
            spawnedTexts.Clear();
        }
    }
}