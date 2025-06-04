using System.Threading.Tasks;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using Loot;

public class UIEffectDisplay : PooledMonoBehaviour, IDraggable, IBeginDragHandler, IDragHandler, IEndDragHandler
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

    public IContainer Container { get; set; }
    public EffectModifier EffectModifier {  get; private set; }

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
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
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, eventData.position, canvas.worldCamera, out Vector2 position);
        rectTransform.localPosition = position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        UIEvents.OnEndDrag?.Invoke(this);
        canvasGroup.blocksRaycasts = true;

        DelayedAdd().Forget();
    }

    private async UniTaskVoid DelayedAdd()
    {
        await UniTask.Yield();

        Container.AddDraggable(this);
    }

    public void Display(EffectModifier effectModifier)
    {
        canvas ??= GetComponentInParent<Canvas>();

        EffectModifier = effectModifier;

        description.text = effectModifier.Description;
        iconImage.sprite = effectModifier.Icon;
        title.text = effectModifier.Title;
    }
}
