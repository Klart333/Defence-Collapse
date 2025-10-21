using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using UnityEngine;
using Variables;

namespace SkillTree.UI
{
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer))]
    public class UIDrawLine : Graphic
    {
        [Title("Color")]
        [SerializeField]
        private ColorReference colorReference;
        
        [Title("Line Points")]
        [SerializeField] 
        private RectTransform fromTransform;
        
        [SerializeField] 
        private RectTransform toTransform;

        [Title("Width")]
        [SerializeField]
        private float width = 0.5f;
        
        private List<UIVertex> _vertices = new();
        private List<int> _indices = new();

        private RectTransform rect;

        protected override void OnEnable()
        {
            base.OnEnable();
            
            rect = GetComponent<RectTransform>();
            color = colorReference.Value;
        }

        private void Update()
        {
            #if UNITY_EDITOR
            color = colorReference.Value;
            #endif
            
            if (!fromTransform || !toTransform) return;

            // Rebuild curve if endpoints move
            if (fromTransform.hasChanged || toTransform.hasChanged)
            {
                fromTransform.hasChanged = false;
                toTransform.hasChanged = false;
                SetVerticesDirty();
            }
        }

        public void SetLinePoints(RectTransform from, RectTransform to)
        {
            fromTransform = from;
            toTransform = to;
            SetVerticesDirty();
        }
        
        private static Vector2 WorldToLocalPoint(RectTransform localRect, Vector3 worldPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(localRect, worldPos, null, out Vector2 localPoint);
            return localPoint;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            if (!fromTransform || !toTransform) return;
            
            vh.Clear();
            _vertices.Clear();
            _indices.Clear();
            
            Vector2 start = WorldToLocalPoint(rect, fromTransform.position);
            Vector2 end = WorldToLocalPoint(rect, toTransform.position);
            
            Vector2 dir = (start - end).normalized;
            Vector2 normal = new Vector2(-dir.y, dir.x) * (width * 0.5f);

            int startIndex = _vertices.Count;
            AddQuad(_vertices, start - normal, start + normal, end + normal, end - normal, color);

            _indices.Add(startIndex);
            _indices.Add(startIndex + 1);
            _indices.Add(startIndex + 2);
            _indices.Add(startIndex);
            _indices.Add(startIndex + 2);
            _indices.Add(startIndex + 3);

            vh.AddUIVertexStream(_vertices, _indices);
        }

        private void AddQuad(List<UIVertex> verts, Vector2 bl, Vector2 tl, Vector2 tr, Vector2 br, Color32 col)
        {
            UIVertex v = UIVertex.simpleVert;
            v.color = col;

            v.position = bl; verts.Add(v);
            v.position = tl; verts.Add(v);
            v.position = tr; verts.Add(v);
            v.position = br; verts.Add(v);
        }

    #if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetVerticesDirty();
        }
    #endif
    }
}