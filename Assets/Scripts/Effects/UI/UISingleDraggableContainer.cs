using UnityEngine.EventSystems;
using Gameplay.Event;
using UnityEngine;
using System;

namespace Effects.UI
{
    public class UISingleDraggableContainer : PooledMonoBehaviour, IContainer, IPointerEnterHandler, IPointerExitHandler
    {
        public event Action<IDraggable> OnDraggableClicked;
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

            if (draggable is IClickable clickable)
            {
                clickable.OnClick += OnDraggableClick;
            }
            
            OnDraggableAdded?.Invoke(draggable);
        }

        public void RemoveDraggable()
        {
            OnDraggableRemoved?.Invoke(containedDraggable);
            
            if (containedDraggable is IClickable clickable)
            {
                clickable.OnClick -= OnDraggableClick;
            }
            
            containedDraggable = null;
        }
        
        private void OnDraggableClick()
        {
            OnDraggableClicked?.Invoke(containedDraggable);
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