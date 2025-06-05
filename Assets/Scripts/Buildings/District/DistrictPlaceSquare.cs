using Unity.Mathematics;
using System;
using InputCamera;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace Buildings.District
{
    public class DistrictPlaceSquare : PooledMonoBehaviour
    {
        private static readonly int Color1 = Shader.PropertyToID("_BaseColor");
        
        public event Action<DistrictPlaceSquare> OnSelected;

        [Title("Display")]
        [SerializeField]
        private MeshRenderer meshRenderer;
    
        [SerializeField]
        private Color defaultColor = Color.white;
    
        [SerializeField]
        private Color hoveredColor = Color.green;
    
        [SerializeField]
        private Color selectedColor = Color.green;

        private MaterialPropertyBlock block;
        
        public bool Selected { get; private set; }

        private bool locked;
        private Color? lastColor;

        public int3 Index { get; set; }

        private void Awake()
        {
            block = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            meshRenderer.GetPropertyBlock(block);
            block.SetColor(Color1, defaultColor);
            meshRenderer.SetPropertyBlock(block);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            lastColor = null;
            locked = false;
            if (Selected)
            {
                Unselect();
            }
        }
        
        private void OnMouseEnter()
        {
            lastColor = block.GetColor(Color1);
            
            block.SetColor(Color1, hoveredColor);
            meshRenderer.SetPropertyBlock(block);
        }

        private void OnMouseExit()
        {
            block.SetColor(Color1, lastColor.GetValueOrDefault(defaultColor));
            meshRenderer.SetPropertyBlock(block);
        }
        
        private void OnMouseUpAsButton()
        {
            if (CameraController.IsDragging || EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            
            OnSelected?.Invoke(this);
        }

        public void ForceSelected()
        {
            SetSelected();
            locked = true;
        }

        public void SetSelected()
        {
            if (locked) return;

            Selected = true;
            
            lastColor = selectedColor;
            block.SetColor(Color1, selectedColor);
            meshRenderer.SetPropertyBlock(block);
        }
        
        public void SetSelected(Color color)
        {
            if (locked) return;

            Selected = true;
            lastColor = color;
            block.SetColor(Color1, color);
            meshRenderer.SetPropertyBlock(block);
        }

        public void Unselect()
        {
            if (locked) return;

            Selected = false;
            
            lastColor = null;
            block.SetColor(Color1, defaultColor);
            meshRenderer.SetPropertyBlock(block);
        }
    }
}