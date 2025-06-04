using System.Collections.Generic;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using UnityEngine;
using System;
using TMPro;
using Loot;

public class UIEffectsHandler : MonoBehaviour, IContainer, IPointerEnterHandler, IPointerExitHandler
{
    public event Action<EffectModifier> OnEffectAdded;
    public event Action<EffectModifier> OnEffectRemoved;

    [Title("Prefabs")]
    [SerializeField]
    private UIEffectDisplay effectDisplayPrefab;

    [Title("UI")]
    [SerializeField]
    private Transform displayParent;

    [SerializeField]
    private TextMeshProUGUI emptyText;

    [Title("Settings")]
    [SerializeField]
    private bool restrictedAmount;

    [SerializeField, ShowIf(nameof(restrictedAmount))]
    private int maxAmount = 3;

    private readonly List<UIEffectDisplay> spawnedDisplays = new List<UIEffectDisplay>();
    
    private UIFlexibleLayoutGroup flexGroup;

    private bool hovered = false;

    public Transform DisplayParent => displayParent;

    private void OnEnable()
    {
        flexGroup = GetComponentInChildren<UIFlexibleLayoutGroup>();

        UIEvents.OnEndDrag += OnEndDrag;
        UIEvents.OnBeginDrag += OnBeginDrag;
    }

    private void OnDisable()
    {
        UIEvents.OnEndDrag -= OnEndDrag;
        UIEvents.OnBeginDrag -= OnBeginDrag;

        for (int i = 0; i < spawnedDisplays.Count; i++)
        {
            spawnedDisplays[i].gameObject.SetActive(false);
        }
        spawnedDisplays.Clear();
    }

    public void SpawnEffects(List<EffectModifier> effects)
    {
        for (int i = effects.Count - 1; i >= 0; i--)
        {
            SpawnEffect(effects[i]);
        }
        
        flexGroup?.CalculateNewBounds();
    }

    private void SpawnEffect(EffectModifier effectModifier)
    {
        emptyText.gameObject.SetActive(false);

        UIEffectDisplay effect = effectDisplayPrefab.Get<UIEffectDisplay>();
        effect.transform.SetParent(displayParent, false);
        effect.Container = this;
        effect.Display(effectModifier);

        spawnedDisplays.Add(effect);
    }

    private void RemoveEffect(UIEffectDisplay effectDisplay)
    {
        spawnedDisplays.Remove(effectDisplay);
        
        OnEffectRemoved?.Invoke(effectDisplay.EffectModifier);
        
        flexGroup?.CalculateNewBounds();
    }

    public void AddDraggable(IDraggable draggable)
    {
        if (draggable is not UIEffectDisplay effectDisplay)
        {
            Debug.LogError("Draggable is not a UIEffectDisplay");
            return;
        }
        
        AddEffect(effectDisplay);
    }

    private void AddEffect(UIEffectDisplay effectDisplay)
    {
        emptyText.gameObject.SetActive(false);

        effectDisplay.transform.SetParent(displayParent);
        effectDisplay.Container = this;

        spawnedDisplays.Add(effectDisplay);

        OnEffectAdded?.Invoke(effectDisplay.EffectModifier);

        flexGroup?.CalculateNewBounds();
    }

    private void OnBeginDrag(IDraggable display)
    {
        if (display is not UIEffectDisplay effectDisplay)
        {
            return;
        }
        
        if (spawnedDisplays.Contains(effectDisplay))
        {
            RemoveEffect(effectDisplay);
        }
    }

    public void OnEndDrag(IDraggable display)
    {
        if (display is not UIEffectDisplay)
        {
            return;
        }
        
        if (hovered && (!restrictedAmount || spawnedDisplays.Count < maxAmount))
        {
            display.Container = this;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
    }
}

public interface IDraggable
{
    public IContainer Container { get; set; }
}

public interface IContainer
{
    public void AddDraggable(IDraggable draggable);
}
