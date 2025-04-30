using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StupidButton : Button
{
    private static readonly int Normal = Animator.StringToHash("Normal");
    
    public UnityEvent OnHoverEnter;
    public UnityEvent OnHoverExit;

    public bool Hovered { get; private set; }

    protected override void OnDisable()
    {
        base.OnDisable();

        Hovered = false;
        if (animator != null)
        {
            animator.SetTrigger(Normal);
        }
    }
    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);

        Hovered = true;
        OnHoverEnter?.Invoke();
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);

        Hovered = false;
        OnHoverExit?.Invoke();
    }

    public void InvokeHoverEvent()
    {
        OnHoverEnter.Invoke();
    }
}
