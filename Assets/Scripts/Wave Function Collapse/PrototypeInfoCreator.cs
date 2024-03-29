using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class PrototypeInfoCreator : MonoBehaviour
{
    [Header("Debug")]
    public bool Debug;

    [SerializeField]
    private PrototypeDisplay prefab;

    [Header("Generated")]
    public List<PrototypeData> Prototypes = new List<PrototypeData>();

    [SerializeField]
    private List<DicData> socketList = new List<DicData>();

    [SerializeField]
    private List<DicData> verticalSocketList = new List<DicData>();

    [SerializeField]
    private List<int> doublePieces = new List<int>();

    [Header("Material")]
    [SerializeField]
    private List<Material> materials = new List<Material>();

    [Header("Weight")]
    [SerializeField]
    private int[] pieceWeights;

    private List<GameObject> spawnedPrototypes = new List<GameObject>();

    private List<Vector3> displayVerts = new List<Vector3>();

    private int currentSideIndex = 0;
    private int currentTopIndex = 0;

    public void CreateInfo()
    {
        Reset();

        MeshFilter[] meshes = GetComponentsInChildren<MeshFilter>();
        for (int i = 0; i < meshes.Length; i++)
        {
            Mesh mesh = meshes[i].sharedMesh;
            List<Vector3> posXs = new List<Vector3>();
            List<Vector3> negXs = new List<Vector3>();
            List<Vector3> posYs = new List<Vector3>();
            List<Vector3> negYs = new List<Vector3>();
            List<Vector3> posZs = new List<Vector3>();
            List<Vector3> negZs = new List<Vector3>();

            Vector3[] verts = mesh.vertices;
            Vector3[] noDupes = verts.Distinct().ToArray();

            for (int g = 0; g < noDupes.Length; g++)
            {
                Vector3 vec = new Vector3(math.round(noDupes[g].x * 100.0f) / 100.0f, math.round(noDupes[g].y * 100.0f) / 100.0f, math.round(noDupes[g].z * 100.0f) / 100.0f);

                if (vec.x == 1f)
                {
                    posXs.Add(vec);
                }

                if (vec.x == -1f)
                {
                    negXs.Add(vec);
                }

                if (vec.y == 1f)
                {
                    posYs.Add(vec);
                }

                if (vec.y == -1f)
                {
                    negYs.Add(vec);
                }

                if (vec.z == 1f)
                {
                    posZs.Add(vec);
                }

                if (vec.z == -1f)
                {
                    negZs.Add(vec);
                }
            }

            string posX = GetSideKey(posXs, 0, i);
            string negX = GetSideKey(negXs, 0, i);
            string posZ = GetSideKey(posZs, 1, i);
            string negZ = GetSideKey(negZs, 1, i);

            // Shit
            {
                if (posX == negX && !posX.Contains('s'))
                {
                    if (posX.Contains('f'))
                    {
                        posX = posX.Replace("f", "");
                    }
                    else
                    {
                        negX += "f";
                    }
                }
            }

            string[] posY = GetTopKeys(posYs);
            string[] negY = GetTopKeys(negYs);

            int[] matIndexes = meshes[i].gameObject.GetComponent<MeshRenderer>().sharedMaterials.Select((x) => materials.IndexOf(x)).ToArray();
            // Add all rotations
            // Need to rotate the vertical too
            PrototypeData prototype0 = new PrototypeData(new MeshWithRotation(mesh, 0), posX, negX, posY[0], negY[0], posZ, negZ, pieceWeights[i], matIndexes);
            PrototypeData prototype1 = new PrototypeData(new MeshWithRotation(mesh, 1), posZ, negZ, posY[1], negY[1], negX, posX, pieceWeights[i], matIndexes);
            PrototypeData prototype2 = new PrototypeData(new MeshWithRotation(mesh, 2), negX, posX, posY[2], negY[2], negZ, posZ, pieceWeights[i], matIndexes);
            PrototypeData prototype3 = new PrototypeData(new MeshWithRotation(mesh, 3), negZ, posZ, posY[3], negY[3], posX, negX, pieceWeights[i], matIndexes);

            Prototypes.Add(prototype0);
            Prototypes.Add(prototype1);
            Prototypes.Add(prototype2);
            Prototypes.Add(prototype3);

            if (doublePieces.Contains(i))
            {
                PrototypeData prototype02 = new PrototypeData(new MeshWithRotation(mesh, 0), negX, posX, posY[0], negY[0], posZ, negZ, pieceWeights[i], matIndexes);
                PrototypeData prototype13 = new PrototypeData(new MeshWithRotation(mesh, 1), posZ, negZ, posY[1], negY[1], posX, negX, pieceWeights[i], matIndexes);
                PrototypeData prototype20 = new PrototypeData(new MeshWithRotation(mesh, 2), posX, negX, posY[2], negY[2], negZ, posZ, pieceWeights[i], matIndexes);
                PrototypeData prototype31 = new PrototypeData(new MeshWithRotation(mesh, 3), negZ, posZ, posY[3], negY[3], negX, posX, pieceWeights[i], matIndexes);

                Prototypes.Add(prototype02);
                Prototypes.Add(prototype13);
                Prototypes.Add(prototype20);
                Prototypes.Add(prototype31);
            }
        }

        // Empty
        Prototypes.Add(new PrototypeData(new MeshWithRotation(null, 0), "-1s", "-1s", "-1s", "-1s", "-1s", "-1s", pieceWeights[meshes.Length], new int[0]));
    }

    private string GetSideKey(List<Vector3> vertexPositions, int mainAxis, int meshIndex)
    {
        if (vertexPositions.Count == 0)
        {
            return "-1s";
        }

        // Project on 2 Dimensional plane
        List<Vector2> positions = new List<Vector2>();
        for (int i = 0; i < vertexPositions.Count; i++)
        {
            if (mainAxis == 0)
            {
                Vector2 pro = new Vector2(vertexPositions[i].z, vertexPositions[i].y);
                positions.Add(pro);

            }
            else if (mainAxis == 1)
            {
                Vector2 pro = new Vector2(vertexPositions[i].x, vertexPositions[i].y);
                positions.Add(pro);
            }
        }

        // Check if already in dictionary
        // First check the strict equals
        for (int i = 0; i < socketList.Count; i++)
        {
            if (this.StrictEquals(socketList[i].positions.ToList(), positions))
            {
                return socketList[i].socketname;
            }
        }

        for (int i = 0; i < socketList.Count; i++)
        {
            if (this.Equals(socketList[i].positions.ToList(), positions))
            {
                return socketList[i].socketname;
            }
        }

        string key = currentSideIndex++.ToString();

        // Check for symmetry
        List<Vector2> negPositions = new List<Vector2>();
        for (int h = 0; h < positions.Count; h++)
        {
            negPositions.Add(new Vector2((2.0f - (positions[h].x + 1) - 1), positions[h].y));

            //float y = (1.0f - (positions[h].y + 0.5f) - 0.5f);
            //strictNegPositions.Add(new Vector2((2.0f - (positions[h].x + 1) - 1), y));
        }

        if (Equals(positions, negPositions))
        {
            key += 's';
        }
        else
        {
            socketList.Add(new DicData(negPositions.ToArray(), key + 'f'));
        }

        socketList.Add(new DicData(positions.ToArray(), key));

        return key;
    }

    private string[] GetTopKeys(List<Vector3> vertexPositions)
    {
        if (vertexPositions.Count == 0)
        {
            return new string[] { "-1s", "-1s", "-1s", "-1s" };
        }

        // Project on 2 Dimensional plane
        List<Vector2> positions = new List<Vector2>();
        for (int i = 0; i < vertexPositions.Count; i++)
        {
            positions.Add(new Vector2(vertexPositions[i].x, vertexPositions[i].z));
        }

        // Check if already in dictionary
        // Check only the first one then copy for the rest
        List<Vector2> poses = Rounded(positions);

        for (int g = 0; g < verticalSocketList.Count; g++)
        {
            if (this.Equals(verticalSocketList[g].positions.ToList(), poses))
            {
                string[] kes = new string[4];
                kes[0] = verticalSocketList[g].socketname;

                if (int.TryParse(kes[0][3].ToString(), out int index))
                {
                    for (int i = 1; i < 4; i++)
                    {
                        int newdex = index + i >= 4 ? index + i - 4 : index + i;
                        kes[i] = string.Format("v{0}_{1}", kes[0][1], newdex);
                    }
                }

                return kes;
            }
        }

        // Add all four rotations
        string[] keys = new string[4];
        for (int i = 0; i < 4; i++)
        {
            string key = string.Format("v{0}_{1}", currentTopIndex, i);
            Vector2[] pos = Rotated(positions, i);
            keys[i] = key;
            verticalSocketList.Add(new DicData(pos, key));
        }

        currentTopIndex++;

        return keys;
    }

    public void Clear()
    {
        Reset();
    }

    public void DisplayPrototypes()
    {
        StopDisplayingPrototypes();

        for (int i = 0; i < Prototypes.Count; i++)
        {
            Vector3 pos = new Vector3(i * 5, 0, 0);
            var prot = Instantiate(prefab, pos, Quaternion.identity);
            prot.Setup(Prototypes[i]);

            spawnedPrototypes.Add(prot.gameObject);
        }
    }

    public void StopDisplayingPrototypes()
    {
        for (int i = 0; i < spawnedPrototypes.Count; i++)
        {
            DestroyImmediate(spawnedPrototypes[i]);
        }

        spawnedPrototypes.Clear();
    }

    private void Reset()
    {
        Prototypes.Clear();
        currentSideIndex = 0;
        currentTopIndex = 0;
        socketList.Clear();
        verticalSocketList.Clear();
        displayVerts.Clear();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        for (int i = 0; i < displayVerts.Count; i++)
        {
            Gizmos.DrawSphere(displayVerts[i], 0.05f);
        }   
    }

    public bool Equals(List<Vector2> vec1, List<Vector2> vec2)
    {
        if (vec1.Count != vec2.Count)
        {
            return false;
        }

        vec1 = Rounded(vec1);
        vec2 = Rounded(vec2);

        for (int i = 0; i < vec2.Count; i++)
        {
            if (!vec1.Contains(vec2[i]))
            {
                return false;
            }
        }

        return true;
    }

    public bool StrictEquals(List<Vector2> vec1, List<Vector2> vec2)
    {
        if (vec1.Count != vec2.Count)
        {
            return false;
        }

        vec1 = Rounded(vec1);
        vec2 = Rounded(vec2);

        for (int i = 0; i < vec2.Count; i++)
        {
            if (vec1[i] != vec2[i])
            {
                return false;
            }
        }

        return true;
    }

    private List<Vector2> Rounded(List<Vector2> vec)
    {
        List<Vector2> rounded = new List<Vector2>();
        for (int i = 0; i < vec.Count; i++)
        {
            rounded.Add(new Vector2(math.round(vec[i].x * 1000.0f) / 1000.0f, math.round(vec[i].y * 1000.0f) / 1000.0f));
        }
        return rounded;
    }

    private Vector2[] Rotated(List<Vector2> positions, int rot)
    {
        if (rot == 0)
        {
            return Rounded(positions).ToArray();
        }

        Vector2[] rotated = new Vector2[positions.Count];
        float angle = rot * 90 * Mathf.Deg2Rad;
        for (int i = 0; i < positions.Count; i++)
        {
            float x = positions[i].x * Mathf.Cos(angle) - positions[i].y * Mathf.Sin(angle);
            float y = positions[i].x * Mathf.Sin(angle) - positions[i].y * Mathf.Cos(angle);
            rotated[i] = new Vector2(x, y);
        }

        return Rounded(rotated.ToList()).ToArray();
    }
}

[System.Serializable]
public struct DicData
{
    public Vector2[] positions;
    public string socketname;

    public DicData(Vector2[] positions, string socketname)
    {
        this.positions = positions;
        this.socketname = socketname;
    }
}

[System.Serializable]
public struct PrototypeData
{
    public MeshWithRotation MeshRot;

    public string PosX;
    public string NegX; 
    public string PosZ;
    public string NegZ;
    public string PosY;
    public string NegY;
    public int Weight;

    public int[] MaterialIndexes;

    public PrototypeData(MeshWithRotation mesh, string posX, string negX, string posY, string negY, string posZ, string negZ, int weight, int[] mats)
    {
        MaterialIndexes = mats;
        MeshRot = mesh;
        PosX = posX;
        NegX = negX;
        PosY = posY;
        NegY = negY;
        PosZ = posZ;
        NegZ = negZ;

        Weight = weight;
    }
}
