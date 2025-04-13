using Unity.Mathematics;
using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Buildings.District
{
    public class DistrictPlacer : PooledMonoBehaviour
    {
        private static readonly int Color1 = Shader.PropertyToID("_BaseColor");
        
        public event Action<DistrictPlacer> OnSelected;

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
        
        private bool selected;

        public int3 Index { get; set; }

        private void OnEnable()
        {
            block = new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(block);
            block.SetColor(Color1, defaultColor);
            meshRenderer.SetPropertyBlock(block);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (selected)
            {
                Unselect();
            }
        }
        
        private void OnMouseEnter()
        {
            if (selected) return;
            
            block.SetColor(Color1, hoveredColor);
            meshRenderer.SetPropertyBlock(block);
        }

        private void OnMouseExit()
        {
            if (selected) return;
            
            block.SetColor(Color1, defaultColor);
            meshRenderer.SetPropertyBlock(block);
        }
        
        private void OnMouseUpAsButton()
        {
            if (CameraController.IsDragging)
            {
                return;
            }
            
            OnSelected?.Invoke(this);
        }

        public void SetSelected()
        {
            selected = true;
            
            block.SetColor(Color1, selectedColor);
            meshRenderer.SetPropertyBlock(block);
        }
        
        public void SetSelected(Color color)
        {
            selected = true;
            
            block.SetColor(Color1, color);
            meshRenderer.SetPropertyBlock(block);
        }

        public void Unselect()
        {
            selected = false;
            
            block.SetColor(Color1, defaultColor);
            meshRenderer.SetPropertyBlock(block);
        }
    }
}