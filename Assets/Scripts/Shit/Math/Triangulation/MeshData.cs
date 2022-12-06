using System.Collections.Generic;
using UnityEngine;

public class MeshData
{
    public Vector3[] vertices;
    public List<int> triangleVertices = new List<int>();

    public MeshData(Vector3[] vertices)
    {
        this.vertices = vertices;
    }

    public MeshData()
    {
        
    }

    public void AddTriangle(int vertexA, int vertexB, int vertexC)
    {
        //Debug.Log(string.Format("{0}, {1}, {2}", vertexA, vertexB, vertexC));
        triangleVertices.Add(vertexA);
        triangleVertices.Add(vertexB);
        triangleVertices.Add(vertexC);
    }

    public Mesh CreateMesh() // Creates the shape of the mesh, the texture is set independently
    {
        //Debug.Log(string.Format("Index count: {0}, Vertex Count: {1}", triangleVertices.Count, vertices.Length));

        Mesh mesh = new Mesh();

        mesh.vertices = vertices;
        mesh.triangles = triangleVertices.ToArray();
        //mesh.uv = uvs;
        mesh.RecalculateNormals();

        return mesh;
    }
}