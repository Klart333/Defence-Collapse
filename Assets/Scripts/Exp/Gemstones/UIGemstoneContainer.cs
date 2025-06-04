using System.Collections.Generic;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Exp.Gemstones
{
    public class UIGemstoneContainer : MonoBehaviour, IContainer, IPointerEnterHandler, IPointerExitHandler
    {
        [Title("Prefabs")]
        [SerializeField]
        private UIGemstone gemstonePrefab;

        [Title("UI")]
        [SerializeField]
        private Transform displayParent;
        
        [Title("Settings")]
        [SerializeField]
        private bool restrictedAmount;

        [SerializeField, ShowIf(nameof(restrictedAmount))]
        private int maxAmount = 1;
        
        [Title("Spawning")]
        [SerializeField]
        private bool shouldSpawnGemstone;

        [SerializeField]
        private bool isActiveGemstones;

        private readonly List<IDraggable> handledDraggables = new List<IDraggable>();
        
        private UIFlexibleLayoutGroup flexGroup;
        private ExpManager expManager;

        private bool hovered;
        
        public int Index { get; set; }

        private void Awake()
        {
            GetExpManager().Forget();
        }

        private async UniTaskVoid GetExpManager()
        {
            expManager = await ExpManager.Get();
            if (shouldSpawnGemstone)
            {
                if (isActiveGemstones)
                {
                    if (expManager.ActiveGemstones.Count > Index)
                    {
                        SpawnGemstone(expManager.ActiveGemstones[Index]);
                        flexGroup?.CalculateNewBounds();
                    }
                }
                else
                {
                    SpawnGemstones(expManager.Gemstones);
                }
            }
        }

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
        }

        public void SpawnGemstones(List<Gemstone> effects)
        {
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                SpawnGemstone(effects[i]);
            }
            
            flexGroup?.CalculateNewBounds();
        }

        private void SpawnGemstone(Gemstone gemstone)
        {
            UIGemstone spawned = Instantiate(gemstonePrefab, displayParent);
            spawned.Container = this;
            spawned.DisplayGemstone(gemstone);

            handledDraggables.Add(spawned);
        }

        public void AddDraggable(IDraggable draggable)
        {
            if (draggable is not UIGemstone gemstone)
            {
                Debug.LogError("Draggable is not a UIEffectDisplay");
                return;
            }
            
            AddGemstone(gemstone);
        }

        private void AddGemstone(UIGemstone gemstone)
        {
            gemstone.transform.SetParent(displayParent);
            gemstone.Container = this;

            handledDraggables.Add(gemstone);
            if (isActiveGemstones)
            {
                expManager.ActiveGemstones.Add(gemstone.Gemstone);
            }
            else
            {
                expManager.Gemstones.Add(gemstone.Gemstone);
            }

            flexGroup?.CalculateNewBounds();
        }
        
        private void RemoveGemstone(UIGemstone gemstone)
        {
            handledDraggables.Remove(gemstone);
            
            if (isActiveGemstones)
            {
                expManager.ActiveGemstones.Remove(gemstone.Gemstone);
            }
            else
            {
                expManager.Gemstones.Remove(gemstone.Gemstone);
            }
            
            flexGroup?.CalculateNewBounds();
        }

        private void OnBeginDrag(IDraggable display)
        {
            if (display is not UIGemstone gemstone)
            {
                return;
            }
            
            if (handledDraggables.Contains(gemstone))
            {
                RemoveGemstone(gemstone);
            }
        }

        public void OnEndDrag(IDraggable display)
        {
            if (display is not UIGemstone)
            {
                return;
            }
            
            if (hovered && (!restrictedAmount || handledDraggables.Count < maxAmount))
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
}