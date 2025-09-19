using Buildings;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using Variables;

namespace Juice
{
    public class TileSelector : PooledMonoBehaviour
    {
        private static readonly int ColorProperty = Shader.PropertyToID("_BaseColor");

        [Title("Color")]
        [SerializeField]
        private MeshRenderer meshRenderer;
        
        [SerializeField]
        private float colorStrength = 1.5f;
        
        [SerializeField]
        private ColorReference defaultColor;
        
        [SerializeField]
        private ColorReference buildableColor;
        
        [SerializeField]
        private ColorReference sellColor;
        
        [SerializeField]
        private ColorReference invalidColor;
        
        [Title("Animation", "Color")]
        [SerializeField]
        private float colorBlendDuration = 0.2f;
        
        [SerializeField]
        private Ease colorBlendEase = Ease.OutQuad;

        private MaterialPropertyBlock block;
        
        private void OnEnable()
        {
            block = new MaterialPropertyBlock();    
            meshRenderer.GetPropertyBlock(block);
            
            BlendColor(defaultColor.Value);
        }
        
        public void Display(Vector3 position)
        {
            transform.position = position;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
        
        public void DisplayAction(TileAction tileAction)
        {
            Color targetColor = tileAction switch
            {
                TileAction.Build => buildableColor.Value,
                TileAction.Sell => sellColor.Value,
                _ => invalidColor.Value,
            };
            BlendColor(targetColor);
        }
        
        private void BlendColor(Color targetColor)
        {
            meshRenderer.DOKill();
            DOTween.To(() => block.GetColor(ColorProperty), value =>
            {
                block.SetColor(ColorProperty, value);
                meshRenderer.SetPropertyBlock(block);
            }, targetColor * colorStrength, colorBlendDuration).SetEase(colorBlendEase).SetId(meshRenderer);
        }
    }
}