using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class Convex : MonoBehaviour
{
    [SerializeField]
    private Transform[] points;

    [SerializeField]
    [Range(0, 2000)]
    private int delay = 1000;

    private List<Vector3> verts = new List<Vector3>();

    private MeshData meshData;
    private Mesh mesh;

    [ContextMenu("Generate")]
    public async void GenerateMesh()
    {
        verts.Clear();

        List<Vector3> allPoints = new List<Vector3>();

        for (int i = 0; i < points.Length; i++)
        {
            allPoints.Add(points[i].position);
            verts.Add(points[i].position);
        }

        meshData = new MeshData(verts.ToArray());

        await Task.Delay(delay);
        int evaq = 0;
        while (allPoints.Count >= 3 && evaq++ < 30)
        {
            await FillIn(allPoints);
            await Task.Delay(delay);
            print("Did one");
        }

        if (allPoints.Count < 3)
        {
            Debug.Log("Completed!");
        }
        else
        {
            Debug.LogError("Had To Evaq");
        }
    }

    private async Task FillIn(List<Vector3> allPoints)
    {
        List<Vector3> convexPoints = GetConvexPoints(allPoints);
        while (convexPoints.Count > 0 && allPoints.Count >= 3)
        {
            TriangleAt(convexPoints, allPoints, 0);
            await Task.Delay(delay);

            if (convexPoints.Count <= 0 || allPoints.Count < 3)
            {
                break;
            }
            TriangleAt(convexPoints, allPoints, convexPoints.Count - 1);
            await Task.Delay(delay);
        }
    }

    private List<Vector3> GetConvexPoints(List<Vector3> points)
    {
        List<Vector3> convexPoints = new List<Vector3>();

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 from, at, to;
            GetPositions(i, out from, out at, out to);

            if (!ReflexPoint(from, at, to))
            {
                convexPoints.Add(points[i]);
            }
        }

        return convexPoints;
    }

    private void TriangleAt(List<Vector3> convexPoints, List<Vector3> allPoints, int i)
    {
        int index = allPoints.IndexOf(convexPoints[i]);
        int forwardIndex = index + 1;
        int backwardIndex = index - 1;

        if (index == 0)
        {
            forwardIndex = index + 1;
            backwardIndex = allPoints.Count - 1;
        }
        else if (index == allPoints.Count - 1)
        {
            forwardIndex = 0;
            backwardIndex = index - 1;
        }

        //print(string.Format("BEFORE: Index: {2} Forward Index: {0}, Backward Index: {1}", forwardIndex, backwardIndex, index));
        index = verts.IndexOf(allPoints[index]);
        forwardIndex = verts.IndexOf(allPoints[forwardIndex]);
        backwardIndex = verts.IndexOf(allPoints[backwardIndex]);
        //print(string.Format("AFTER: Index: {2} Forward Index: {0}, Backward Index: {1}", forwardIndex, backwardIndex, index));

        //print( string.Format("Index: {0}, Forward Index: {1}, Backward Index: {2}", index, forwardIndex, backwardIndex));
        if (ChecksOut(index, forwardIndex, backwardIndex))
        {
            meshData.AddTriangle(index, forwardIndex, backwardIndex);

            allPoints.RemoveAt(allPoints.IndexOf(convexPoints[i]));
            convexPoints.RemoveAt(i);

            mesh = meshData.CreateMesh();
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }
        else
        {
            //allPoints.RemoveAt(allPoints.IndexOf(convexPoints[i]));
            convexPoints.RemoveAt(i);
        }
    }

    private bool ChecksOut(int index, int ind1, int ind2)
    {
        bool checksOut = true;
        for (int h = 0; h < points.Length; h++)
        {
            if (points[h].position == verts[index] || points[h].position == verts[ind1] || points[h].position == verts[ind2])
            {
                continue;
            }

            if (IsInsideTriangle(points[h].position, verts[index], verts[ind1], verts[ind2]))
            {
                checksOut = false;
            }
        }

        return checksOut;
    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < points.Length; i++)
        {
            Vector3 from, at, to;
            GetPositions(i, out from, out at, out to);

            if (ReflexPoint(from, at, to))
            {
                Gizmos.color = Color.cyan;
            }
            else
            {
                Gizmos.color = Color.green;
            }

            Gizmos.DrawSphere(at, 0.1f);

            Gizmos.color = Color.black;
            Gizmos.DrawLine(at, from);
            Handles.Label(at + Vector3.forward * 0.5f, i.ToString());
        }

        //GenerateMesh();
    }

    private void GetPositions(int i, out Vector3 from, out Vector3 at, out Vector3 to)
    {
        at = points[i].localPosition;
        if (i == 0)
        {
            from = points[points.Length - 1].localPosition;
            to = points[i + 1].localPosition;
        }
        else if (i == points.Length - 1)
        {
            from = points[i - 1].localPosition;
            to = points[0].localPosition;
        }
        else
        {
            from = points[i - 1].localPosition;
            to = points[i + 1].localPosition;
        }
    }

    private bool ReflexPoint(Vector3 from, Vector3 at, Vector3 to)
    {
        return WedgeProduct(at, from, to) < 0;
    }

    private float WedgeProduct(Vector3 point, Vector3 from, Vector3 to)
    {
        Vector3 dir = (to - from).normalized;
        Vector3 midPoint = Vector3.Lerp(from, to, 0.5f);
        Vector3 perpendicular = Quaternion.AngleAxis(90, Vector3.forward) * dir;

        return Vector3.Dot(perpendicular, point - midPoint);
    }

    private bool IsInsideTriangle(Vector2 point, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        float x = WedgeProduct(point, p1, p2);
        float y = WedgeProduct(point, p2, p3);
        float z = WedgeProduct(point, p3, p1);

        return Mathf.Sign(x) == Mathf.Sign(y) && Mathf.Sign(y) == Mathf.Sign(z);
    }
}
