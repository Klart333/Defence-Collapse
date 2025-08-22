using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;
using UnityEngine.EventSystems;

namespace UI
{
    public class UITooltipDisplay : PooledMonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public event Action OnPointerEnter;
        public event Action OnPointerExit;
        
        [SerializeField]
        private Transform contentParent;
        
        [SerializeField]
        private TextMeshProUGUI tooltipTextPrefab;

        private Queue<TextMeshProUGUI> textPool = new Queue<TextMeshProUGUI>();
        private List<TextMeshProUGUI> spawnedTexts = new List<TextMeshProUGUI>();

        public void DisplayTooltip(IEnumerable<Tuple<string, int>> tooltips)
        {
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
            for (int i = 0; i < spawnedTexts.Count; i++)
            {
                spawnedTexts[i].gameObject.SetActive(false);
                textPool.Enqueue(spawnedTexts[i]);
            }
            spawnedTexts.Clear();
            
            gameObject.SetActive(false);
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            OnPointerEnter?.Invoke();
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            OnPointerExit.Invoke();
        }
    }
}