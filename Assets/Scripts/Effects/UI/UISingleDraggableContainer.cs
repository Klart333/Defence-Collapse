using UnityEngine.EventSystems;
using Gameplay.Event;
using UnityEngine;
using System;

namespace Effects.UI
{
    public class UISingleDraggableContainer : PooledMonoBehaviour, IContainer, IPointerEnterHandler, IPointerExitHandler
    {
        public event Action<IDraggable> OnDraggableAdded;
        public event Action<IDraggable> OnDraggableRemoved;
        
        [SerializeField]
        private Transform draggableParent;
        
        private IDraggable containedDraggable;
        
        private bool hovered;

        public bool HasDraggable => containedDraggable != null;
        public Transform DraggableParent => draggableParent;
        
        private void OnEnable()
        {
            UIEvents.OnEndDrag += OnEndDrag;
            UIEvents.OnBeginDrag += OnBeginDrag;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            UIEvents.OnEndDrag -= OnEndDrag;
            UIEvents.OnBeginDrag -= OnBeginDrag;

            containedDraggable?.Disable();
            containedDraggable = null;
        }

        private void OnBeginDrag(IDraggable draggable)
        {
            if (containedDraggable == draggable)
            {
                RemoveDraggable();
            }
        }

        public void OnEndDrag(IDraggable display)
        {
            if (hovered && !HasDraggable)
            {
                display.Container = this;
            }
        }
        
        public void AddDraggable(IDraggable draggable)
        {
            containedDraggable = draggable;
            containedDraggable.Container = this;
            containedDraggable.SetParent(draggableParent);
            
            OnDraggableAdded?.Invoke(draggable);
        }
        
        private void RemoveDraggable()
        {
            OnDraggableRemoved?.Invoke(containedDraggable);
            
            containedDraggable = null;
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