using System;
using Unity.Mathematics;
using UnityEngine;
using WaveFunctionCollapse;

public class PlaceSquare : MonoBehaviour
{
    private static readonly int Color1 = Shader.PropertyToID("_BaseColor");

    [SerializeField]
    private MeshRenderer meshRenderer;
    
    [SerializeField]
    private Color defaultColor = Color.white;
    
    [SerializeField]
    private Color hoveredColor = Color.green;
    
    private MaterialPropertyBlock block;
    
    public bool Placed { get; set; }
    public bool Locked { get; set; }

    private void Awake()
    {
        block = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        if (Placed || Locked)
        {
            gameObject.SetActive(false);
            return;
        }
        
        meshRenderer.GetPropertyBlock(block);
        block.SetColor(Color1, defaultColor);
        meshRenderer.SetPropertyBlock(block);
    }

    public void OnHover()
    {
        if (Placed || Locked) return;
        
        block.SetColor(Color1, hoveredColor);
        meshRenderer.SetPropertyBlock(block);
    }

    public void OnHoverExit()
    {
        if (Placed || Locked) return;
        
        block.SetColor(Color1, defaultColor);
        meshRenderer.SetPropertyBlock(block);
    }

    public void OnPlaced()
    {
        Placed = true;
        gameObject.SetActive(false);
    }

    public void UnPlaced()
    {
        Placed = false;
        gameObject.SetActive(true);
    }
}