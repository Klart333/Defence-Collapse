using System.Collections.Generic;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;

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
        
        [Title("Animation")]
        [SerializeField]
        private float fadeInDuration = 0.2f;

        [SerializeField]
        private float fadeOutDuration = 0.2f;
        
        [SerializeField]
        private Ease ease = Ease.InOutCirc;

        private Queue<TextMeshProUGUI> textPool = new Queue<TextMeshProUGUI>();
        private List<TextMeshProUGUI> spawnedTexts = new List<TextMeshProUGUI>();
        
        private CanvasGroup canvasGroup;
        private Canvas canvas;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvas = GetComponentInParent<Canvas>();
        }

        public void DisplayTooltip(ICollection<TextData> tooltips, bool blocksRaycasts)
        {
            canvasGroup.blocksRaycasts = blocksRaycasts;
            
            FadeTo(fadeInDuration, 1);

            foreach (TextData tooltip in tooltips)
            {
                TextMeshProUGUI text = GetText();
                text.transform.SetAsLastSibling();
                text.text = tooltip.Text;
                text.fontSize = tooltip.FontSize;
            }

            DelayedClampToCanvas().Forget();
        }

        private void FadeTo(float duration, float alpha)
        {
            canvasGroup.DOKill();
            canvasGroup.DOFade(alpha, duration).SetEase(ease);
        }

        private async UniTaskVoid DelayedClampToCanvas()
        {
            await UniTask.Yield();
            (transform as RectTransform).ClampToParent(canvas.transform as RectTransform);
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

        public async UniTaskVoid HideTooltip()
        {
            FadeTo(fadeOutDuration, 0);
            await UniTask.Delay(TimeSpan.FromSeconds(fadeOutDuration));
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