using UnityEngine;
using UnityEngine.EventSystems;

public class UIMenuOption : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField]
    private UIMenuPanel menuPanel;

    public void OnPointerEnter(PointerEventData eventData)
    {
        menuPanel.HoverOption(transform as RectTransform);
    }
}
