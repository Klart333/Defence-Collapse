using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StupidButton : Button
{
    public bool Hovered { get; private set; }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);

        Hovered = true;
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);

        Hovered = false;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        Hovered = false;
    }
}
