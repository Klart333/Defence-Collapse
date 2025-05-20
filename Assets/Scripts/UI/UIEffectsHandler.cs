using System.Collections.Generic;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using UnityEngine;
using System;
using TMPro;
using Loot;

public class UIEffectsHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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
        effect.Handler = this;
        effect.Display(effectModifier);

        spawnedDisplays.Add(effect);
    }

    private void RemoveEffect(UIEffectDisplay effectDisplay)
    {
        spawnedDisplays.Remove(effectDisplay);
        
        OnEffectRemoved?.Invoke(effectDisplay.EffectModifier);
        
        flexGroup?.CalculateNewBounds();
    }

    public void AddEffectDisplay(UIEffectDisplay effectDisplay)
    {
        emptyText.gameObject.SetActive(false);

        effectDisplay.transform.SetParent(displayParent);
        effectDisplay.Handler = this;

        spawnedDisplays.Add(effectDisplay);

        OnEffectAdded?.Invoke(effectDisplay.EffectModifier);

        flexGroup?.CalculateNewBounds();
    }

    private void OnBeginDrag(UIEffectDisplay display)
    {
        if (spawnedDisplays.Contains(display))
        {
            RemoveEffect(display);
        }
    }

    public void OnEndDrag(UIEffectDisplay display)
    {
        if (hovered && (!restrictedAmount || spawnedDisplays.Count < maxAmount))
        {
            display.Handler = this;
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
