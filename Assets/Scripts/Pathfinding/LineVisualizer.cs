using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Linq;
using UnityEngine;

namespace Pathfinding
{
    public class LineVisualizer : PooledMonoBehaviour
    {
        [SerializeField]
        private LineRenderer lineRenderer;

        [SerializeField]
        private float delay = 0.05f;
        
        public PathIndex StartIndex { get; set; }
        
        private Vector3[] currentPoints;

        public void DisplayLine(List<Vector3> points)
        {
            Vector3[] newPoints = points.ToArray();
            if (currentPoints != null && CheckEquality(currentPoints, newPoints))
            {
                return;
            }
            
            currentPoints = newPoints;
            DisplayPointsAnimated(newPoints).Forget();
        }

        private async UniTaskVoid DisplayPointsAnimated(Vector3[] newPoints)
        {
            for (int i = 0; i < newPoints.Length; i++)
            {
                lineRenderer.positionCount = i;
                lineRenderer.SetPositions(newPoints.Take(i).ToArray());
                await UniTask.Delay(TimeSpan.FromSeconds(delay / newPoints.Length));
            }
        }

        private bool CheckEquality(Vector3[] oldPoints, Vector3[] newPoints)
        {
            if (oldPoints.Length != newPoints.Length)
            {
                return false;
            }

            for (int i = 0; i < oldPoints.Length; i++)
            {
                if ((oldPoints[i] - newPoints[i]).sqrMagnitude > 0.001f)
                {
                    return false;
                }
            }

            return true;
        }
    }
}