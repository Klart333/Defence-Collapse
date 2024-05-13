using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIEffectDisplay : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Title("References")]
    [SerializeField]
    private TextMeshProUGUI description;

    [SerializeField]
    private TextMeshProUGUI title;

    [SerializeField]
    private Image iconImage;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Canvas canvas;

    public UIEffectsHandler Handler { get; set; }
    public EffectModifier EffectModifier {  get; private set; }
    public bool Locked { get; set; }

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>(); // Ensure there is a Canvas in the parent hierarchy.
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        UIEvents.OnBeginDrag?.Invoke(this);

        transform.SetParent(canvas.transform);

        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Convert the drag delta to canvas scale
        Vector2 position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, eventData.position, canvas.worldCamera, out position);
        rectTransform.localPosition = position;
    }

    public async void OnEndDrag(PointerEventData eventData)
    {
        UIEvents.OnEndDrag?.Invoke(this);
        canvasGroup.blocksRaycasts = true;

        await UniTask.Yield();

        Handler.AddEffectDisplay(this);
    }

    public void Display(EffectModifier effectModifier)
    {
        EffectModifier = effectModifier;

        description.text = effectModifier.Description;
        iconImage.sprite = effectModifier.Icon;
        title.text = effectModifier.Title;
    }

}
