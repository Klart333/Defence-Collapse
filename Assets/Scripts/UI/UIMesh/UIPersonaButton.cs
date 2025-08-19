using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using UnityEngine;

namespace UI.UIMesh
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class UIPersonaButton : Graphic
    {
        [Title("Colors")]
        [SerializeField]
        private Color mainColor;
        
        [SerializeField]
        private Color secondaryColor;
        
        private List<UIVertex> vertices = new List<UIVertex>();
        private List<int> indices = new List<int>();

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            vertices.Clear();
            indices.Clear();
                
            OnRender(vertices, indices);

            vh.AddUIVertexStream(vertices, indices);
        }

        private void OnRender(List<UIVertex> vertices, List<int> indices)
        {
            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;
            Vector3 offset = new Vector3(width / 2.0f, height / 2.0f, 0);

            vertices.Add(new UIVertex { color = color, position = new Vector3(0, 0, 0) - offset });
            vertices.Add(new UIVertex { color = color, position = new Vector3(width, 0, 0) - offset });
            vertices.Add(new UIVertex { color = color, position = new Vector3(0, height, 0) - offset });
            vertices.Add(new UIVertex { color = color, position = new Vector3(width, height, 0) - offset });
            
            indices.Add(0);
            indices.Add(1);
            indices.Add(3);
            
            indices.Add(0);
            indices.Add(2);
            indices.Add(3);
        }

        public override bool Raycast(Vector2 sp, Camera eventCamera)
        {
            return true;
        }
    }
}