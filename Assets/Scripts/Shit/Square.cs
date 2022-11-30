using System;
using UnityEngine;

namespace Path
{
    public class Square : MonoBehaviour
    {
        private SpriteRenderer spriteRender;
        public void SetColor(Color color)
        {
            if (spriteRender == null)
            {
                spriteRender = GetComponent<SpriteRenderer>();
            }
            spriteRender.color = color;
        }
    }
}

