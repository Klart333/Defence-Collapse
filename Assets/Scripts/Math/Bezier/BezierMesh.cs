using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class BezierMesh : MonoBehaviour
{
    [SerializeField]
    private Transform[] mainPoints;

    [SerializeField]
    private int detailPerSegment = 20;

    [SerializeField]
    private float meshWidth;

    [SerializeField]
    private float meshHeight;

    [SerializeField]
    private float flare = 0.1f;

    private List<Vector3> verts = new List<Vector3>();

    private MeshData meshData;
    private Mesh mesh;

    [ContextMenu("Generate")]
    public void GenerateMesh()
    {
        verts.Clear();
        meshData = new MeshData();

        for (int i = 0; i < mainPoints.Length - 1; i++)
        {
            for (int g = 0; g < detailPerSegment; g++)
            {
                float v = (float)g / (float)(detailPerSegment - 1.0f);
                float v1 = (float)(g + 0.5f) / (float)(detailPerSegment - 1.0f) + 0.1f * flare;

                Vector3 sample = SampleCurve(i, v);
                Vector3 forwardSample = SampleCurve(i, v1);

                Vector3 forward = (forwardSample - sample).normalized;
                Vector3 left = (Quaternion.AngleAxis(90, Vector3.forward) * forward).normalized;
                Vector3 up = Vector3.Cross(forward, left).normalized;
                left = Vector3.Cross(up, forward).normalized;

                // Place Vertices
                for (int h = 0; h < 2; h++)
                {
                    Vector3 upPos = up * meshHeight * h;
                    Vector3 leftPos = left * meshWidth;

                    Vector3 pos0 = sample + leftPos + upPos;
                    Vector3 pos1 = sample + -leftPos + upPos;
                    Vector3 pos2 = forwardSample + leftPos + upPos;
                    Vector3 pos3 = forwardSample + -leftPos + upPos;

                    verts.Add(pos0);
                    verts.Add(pos1);
                    verts.Add(pos2);
                    verts.Add(pos3);

                    // Count = 4
                    if (h == 1)
                    {
                        meshData.AddTriangle(verts.Count - 2, verts.Count - 4, verts.Count - 3);
                        meshData.AddTriangle(verts.Count - 2, verts.Count - 3, verts.Count - 1);
                    }
                    else
                    {
                        meshData.AddTriangle(verts.Count - 2, verts.Count - 3, verts.Count - 4);
                        meshData.AddTriangle(verts.Count - 2, verts.Count - 1, verts.Count - 3);
                    }
                }

                // Tape Worm
                {
                    //meshData.AddTriangle(verts.Count - 8, verts.Count - 4, verts.Count - 7);
                    //meshData.AddTriangle(verts.Count - 4, verts.Count - 3, verts.Count - 7);
                    //
                    //meshData.AddTriangle(verts.Count - 2, verts.Count - 6, verts.Count - 5);
                    //meshData.AddTriangle(verts.Count - 2, verts.Count - 5, verts.Count - 1);
                }

                // Count = 8
                meshData.AddTriangle(verts.Count - 3, verts.Count - 7, verts.Count - 5);
                meshData.AddTriangle(verts.Count - 3, verts.Count - 5, verts.Count - 1);
                
                meshData.AddTriangle(verts.Count - 4, verts.Count - 6, verts.Count - 8);
                meshData.AddTriangle(verts.Count - 4, verts.Count - 2, verts.Count - 6);

                // Connect the segments
                if (g > 0 && g < detailPerSegment - 1)
                {
                    // Connect backwards, Count = 16
                    // Top
                    meshData.AddTriangle(verts.Count - 10, verts.Count - 3, verts.Count - 4);
                    meshData.AddTriangle(verts.Count - 10, verts.Count - 9, verts.Count - 3);
                    
                    // Bot
                    meshData.AddTriangle(verts.Count - 13, verts.Count - 14, verts.Count - 7);
                    meshData.AddTriangle(verts.Count - 14, verts.Count - 8, verts.Count - 7);
                    
                    // Side
                    meshData.AddTriangle(verts.Count - 10, verts.Count - 4, verts.Count - 8);
                    meshData.AddTriangle(verts.Count - 10, verts.Count - 8, verts.Count - 14);
                    
                    // Other Side
                    meshData.AddTriangle(verts.Count - 9, verts.Count - 7, verts.Count - 3);
                    meshData.AddTriangle(verts.Count - 9, verts.Count - 13, verts.Count - 7);
                }
            }
        }

        meshData.vertices = verts.ToArray();

        mesh = meshData.CreateMesh();
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < verts.Count; i++)
        {
            //Handles.Label(verts[i], i.ToString());
        }

        for (int i = 0; i < mainPoints.Length; i++)
        {
            Gizmos.color = Color.red;
            Handles.Label(mainPoints[i].position + Vector3.up * 1f, i.ToString());

            Gizmos.DrawSphere(mainPoints[i].position, 0.3f);

            for (int g = 0; g < mainPoints[i].childCount; g++)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawLine(mainPoints[i].position, mainPoints[i].GetChild(g).position);
                Gizmos.DrawSphere(mainPoints[i].GetChild(g).position, 0.2f);
            }
        }

        for (int i = 0; i < mainPoints.Length - 1; i++)
        {
            for (int g = 0; g < detailPerSegment; g++)
            {
                float v = (float)g / (float)(detailPerSegment - 1.0f);
                float v1 = (float)(g + 1.0f) / (float)(detailPerSegment - 1.0f) + 0.1f * flare;

                Vector3 sample = SampleCurve(i, v);
                Vector3 forwardSample = SampleCurve(i, v1);

                Vector3 dir = (forwardSample - sample).normalized;
                Vector3 left = (Quaternion.AngleAxis(90, Vector3.forward) * dir);
                Vector3 up = (Vector3.Cross(dir, left));
                left = Vector3.Cross(up, dir);

                Gizmos.color = Color.red;
                Gizmos.DrawRay(sample, left);

                Gizmos.color = Color.green;
                Gizmos.DrawRay(sample, up);

                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(sample, dir);

                Gizmos.color = Color.white;
                Gizmos.DrawLine(sample, forwardSample);
            }
        }
    }

    private Vector3 SampleCurve(int i, float t)
    {
        return QuadraticLerp(mainPoints[i].position, mainPoints[i + 1].position, mainPoints[i].GetChild(1).position, mainPoints[i + 1].GetChild(0).position, t);
    }

    private Vector3 QuadraticLerp(Vector3 from, Vector3 to, Vector3 up1, Vector3 up2, float t)
    {
        Vector3 lerp1 = CubicLerp(from, up2, up1, t);
        Vector3 lerp2 = CubicLerp(up1, to, up2, t);

        return Vector3.Lerp(lerp1, lerp2, t);
    }

    private Vector3 CubicLerp(Vector3 from, Vector3 to, Vector3 up, float t)
    {
        Vector3 lerp1 = Lerp(from, up, t);
        Vector3 lerp2 = Lerp(up, to, t);

        return Vector3.Lerp(lerp1, lerp2, t);
    }

    private Vector3 Lerp(Vector3 from, Vector3 to, float t)
    {
        return Vector3.Lerp(from, to, t);
    }
}
