using System;
using UnityEngine;

public class Hovered : MonoBehaviour
{ 
    public bool IsHovered { get; private set; }

    private void OnDisable()
    {
        IsHovered = false;
    }

    private void OnMouseEnter()
    {
        IsHovered = true;
    }

    private void OnMouseExit()
    {
        IsHovered = false;
    }
}
