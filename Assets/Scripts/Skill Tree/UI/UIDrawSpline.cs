using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using UnityEngine;
using Variables;
using Utility;

namespace SkillTree.UI
{
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer))]
    public class UIDrawSpline : Graphic
    {
        [Title("Color")]
        [SerializeField]
        private ColorReference colorReference;
        
        [Title("Spline Points")]
        [SerializeField] 
        private RectTransform fromTransform;
        
        [SerializeField] 
        private RectTransform toTransform;

        [Title("Curve Settings")]
        [SerializeField]
        private Vector2 offset;
            
        [SerializeField, Range(-1f, 1f)] 
        private float controlTension = 0.5f;
        
        [SerializeField, Range(2, 100)] 
        private int segments = 20;
        
        [SerializeField, Min(0.1f)] 
        private float lineWidth = 4f;

        private List<UIVertex> _vertices = new();
        private List<int> _indices = new();

        private Vector2[] _curvePoints;
        
        protected override void OnEnable()
        {
            base.OnEnable();
            CreateCurve();
            
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
                CreateCurve();
                SetVerticesDirty();
            }
        }

        private void CreateCurve()
        {
            if (!fromTransform || !toTransform) return;

            // Convert world positions to local space of this graphic
            RectTransform rect = rectTransform;
            Vector2 start = WorldToLocalPoint(rect, fromTransform.position);
            Vector2 end = WorldToLocalPoint(rect, toTransform.position);

            // Control point offset direction
            Vector2 dir = (end - start);
            Vector2 controlOffset = new Vector2(-dir.y, dir.x).normalized * (dir.magnitude * controlTension);

            Vector2 control1 = start + controlOffset;
            Vector2 control2 = end + controlOffset;

            _curvePoints = new Vector2[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                _curvePoints[i] = BezierMath.CalculateCubicBezierPoint(t, start, control1, control2, end) + offset;
            }
        }

        private static Vector2 WorldToLocalPoint(RectTransform localRect, Vector3 worldPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(localRect, worldPos, null, out Vector2 localPoint);
            return localPoint;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            _vertices.Clear();
            _indices.Clear();

            if (_curvePoints == null || _curvePoints.Length < 2)
                return;

            for (int i = 0; i < _curvePoints.Length - 1; i++)
            {
                Vector2 p0 = _curvePoints[i];
                Vector2 p1 = _curvePoints[i + 1];
                Vector2 dir = (p1 - p0).normalized;
                Vector2 normal = new Vector2(-dir.y, dir.x) * (lineWidth * 0.5f);

                int startIndex = _vertices.Count;
                if (i > 0)
                {
                    AddQuad(_vertices, _vertices[^1].position, _vertices[^2].position, p1 + normal, p1 - normal, color);
                }
                else
                {
                    AddQuad(_vertices, p0 - normal, p0 + normal, p1 + normal, p1 - normal, color);
                }

                _indices.Add(startIndex);
                _indices.Add(startIndex + 1);
                _indices.Add(startIndex + 2);
                _indices.Add(startIndex);
                _indices.Add(startIndex + 2);
                _indices.Add(startIndex + 3);
            }

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
            CreateCurve();
            SetVerticesDirty();
        }
    #endif
    }
}