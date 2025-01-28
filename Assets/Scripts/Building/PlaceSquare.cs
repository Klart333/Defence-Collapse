using System;
using UnityEngine;

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
    
    public BuildingPlacer Placer { get; set; }
    public Vector3Int Index { get; set; }
    public int SquareIndex { get; set; }
    private bool Placed { get; set; }

    private void OnEnable()
    {
        if (Placed)
        {
            gameObject.SetActive(false);
            return;
        }
        
        block = new MaterialPropertyBlock();
        meshRenderer.GetPropertyBlock(block);
        block.SetColor(Color1, defaultColor);
        meshRenderer.SetPropertyBlock(block);
    }

    private void OnMouseEnter()
    {
        if (Placed) return;
        
        block.SetColor(Color1, hoveredColor);
        meshRenderer.SetPropertyBlock(block);
        Placer.SquareIndex = Index;
        Placer.SpawnSquareIndex = SquareIndex;
    }

    private void OnMouseExit()
    {
        if (Placed) return;
        
        block.SetColor(Color1, defaultColor);
        meshRenderer.SetPropertyBlock(block);
        Placer.SquareIndex = null;
    }

    public void OnPlaced()
    {
        Placed = true;
        gameObject.SetActive(false);
    }
}