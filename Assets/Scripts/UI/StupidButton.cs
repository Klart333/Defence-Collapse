using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StupidButton : Button
{
    private static readonly int Normal = Animator.StringToHash("Normal");
    
    public UnityEvent OnHoverEvent;

    public bool Hovered { get; private set; }

    protected override void OnDisable()
    {
        base.OnDisable();

        Hovered = false;
        animator.SetTrigger(Normal);
    }
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

    public void InvokeHoverEvent()
    {
        OnHoverEvent.Invoke();
    }
}
