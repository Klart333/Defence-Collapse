using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using UnityEngine;

public class UIDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Title("Click")]
    [SerializeField]
    private float percentClickable = 0.7f;

    [Title("Settings")]
    [SerializeField]
    private RectTransform connectedElement;

    [SerializeField]
    private bool shouldSetParentToCanvas = true;

    private Vector2 connectedDelta;
    private Vector2 pointerOffset;

    private RectTransform rectTransform;
    private Canvas canvas;

    private bool isDragging = false;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>(); 

        if (connectedElement != null)
        {
            connectedDelta = connectedElement.localPosition - rectTransform.localPosition;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, null, out pointerOffset);
        pointerOffset *= canvas.scaleFactor;
        if (pointerOffset.y < (rectTransform.rect.height / 2f) * percentClickable)
        {
            return;
        }

        isDragging = true;

        if (shouldSetParentToCanvas)
        {
            transform.SetParent(canvas.transform);
        }

        if (connectedElement != null && shouldSetParentToCanvas)
        {
            connectedElement.SetParent(canvas.transform);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            return;
        }

        Vector2 pointerPosition = ClampToCanvas(eventData.position);
        rectTransform.position = pointerPosition - pointerOffset;

        if (connectedElement != null)
        {
            connectedElement.position = pointerPosition - pointerOffset + connectedDelta;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
    }

    // Ensure the UI element stays within the canvas bounds
    private Vector2 ClampToCanvas(Vector2 position)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, position, canvas.worldCamera, out Vector2 localPointerPosition);
        Rect canvasRect = canvas.GetComponent<RectTransform>().rect;
        Vector2 clampedPosition = new Vector2(
            Mathf.Clamp(localPointerPosition.x, canvasRect.xMin, canvasRect.xMax),
            Mathf.Clamp(localPointerPosition.y, canvasRect.yMin, canvasRect.yMax)
        );
        return canvas.transform.TransformPoint(clampedPosition);
    }
}
