using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using DG.Tweening;
using InputCamera;
using UnityEngine;
using Variables;
using Buildings;

namespace Juice
{
    public class SelectedTileHandler : MonoBehaviour
    {
        private static readonly int ColorProperty = Shader.PropertyToID("_BaseColor");

        [Title("Setup")]
        [SerializeField]
        private GameObject selectedTile;
        
        [SerializeField]
        private Vector3 offset = new Vector3(0, -0.1f, 0);
        
        [Title("References")]
        [SerializeField]
        private GroundGenerator groundGenerator;

        [SerializeField]
        private InputEntityWriter inputWriter;
        
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

        [Title("Animation", "Movement")]
        [SerializeField]
        private float smoothMovementHeight = 0.2f;
        
        [SerializeField]
        private float smoothMovementDuration = 0.2f;
        
        [SerializeField]
        private Ease smoothMovementEase = Ease.OutQuad;
        
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

        public void SelectTile(ChunkIndex chunkIndex)
        {
            Vector3 pos = ChunkWaveUtility.GetPosition(chunkIndex, groundGenerator.ChunkScale, groundGenerator.ChunkWaveFunction.CellSize); 
            SelectTile(pos + groundGenerator.ChunkWaveFunction.CellSize.XyZ(0) / 2.0f);
        }

        public void SelectTile(Vector3 position)
        {
            if (selectedTile.activeSelf)
            {
                selectedTile.transform.DOKill();
                selectedTile.transform.position += Vector3.up * smoothMovementHeight;
                selectedTile.transform.DOMove(position + offset, smoothMovementDuration).SetEase(smoothMovementEase);
            }
            else
            {
                selectedTile.transform.position = position + offset;
                selectedTile.SetActive(true);
            }
            
            inputWriter.OverrideShaderMousePosition(position);
        }

        public void Hide()
        {
            selectedTile.SetActive(false);
            
            inputWriter.DisableOverride();
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