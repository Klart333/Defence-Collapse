using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Unity.Mathematics;
using UnityEngine;
using System.Linq;
using System;

public class PrototypeInfoCreator : SerializedMonoBehaviour
{
    [Title("Debug")]
    public bool Debug;

    [SerializeField]
    private PrototypeDisplay prefab;

    [SerializeField]
    private float divider = 100f;

    [Title("Override")]
    [SerializeField]
    private bool overrideVerticalPositive = false;

    [SerializeField, ShowIf(nameof(overrideVerticalPositive))]
    private string positiveKey = "-1s";

    [SerializeField]
    private bool overrideVerticalNegative = false;

    [SerializeField, ShowIf(nameof(overrideVerticalNegative))]
    private string negativeKey = "v-1_0";

    [Title("Rules")]
    [SerializeField]
    private List<int> notAllowedBottomIndexes;
    
    [SerializeField]
    private List<int> onlyAllowedBottomIndexes;

    [SerializeField]
    private List<int> notAllowedSideIndexes;

    [SerializeField]
    private int castlePrototypeIndex;

    [Title("Settings")]
    [SerializeField]
    private Vector3 moduleScale;

    [SerializeField]
    private bool useMCode = false;

    [SerializeField]
    [ShowIf(nameof(useMCode))]
    private bool useMCodeHeight = false;
    
    [SerializeField]
    private bool useBuildableCorners = false;

    [SerializeField, ShowIf(nameof(useBuildableCorners))]
    private string rightBuildableCode = "0";
    
    [SerializeField, ShowIf(nameof(useBuildableCorners))]
    private BuildableCornerData buildableCornerData;    
    
    [Title("Generated")]
    public List<PrototypeData> Prototypes = new List<PrototypeData>();

    [SerializeField]
    private List<DicData> socketList = new List<DicData>();

    [SerializeField]
    private List<DicData> verticalSocketList = new List<DicData>();

    [SerializeField]
    private List<int> notAllowedForBottom = new List<int>();

    [SerializeField]
    private List<int> onlyAllowedForBottom = new List<int>();
    
    [SerializeField]
    private List<int> notAllowedForSides = new List<int>();

    [SerializeField, ShowIf(nameof(useMCode))]
    private List<PrototypeData>[] marchingTable;
    
    [SerializeField]
    private int castleIndex;

    [Title("Material")]
    [SerializeField]
    private MaterialData materialData;

    [Title("Weight")]
    [SerializeField]
    private List<StupidWeightThing> pieceWeights;

    private List<GameObject> spawnedPrototypes = new List<GameObject>();

    private int currentSideIndex = 0;
    private int currentTopIndex = 0;

    public List<int> OnlyAllowedForBottom => onlyAllowedForBottom;
    public List<int> NotAllowedForBottom => notAllowedForBottom;
    public List<PrototypeData>[] MarchingTable => marchingTable;
    public List<int> NotAllowedForSides => notAllowedForSides;
    public int CastleIndex => castleIndex;

    [TitleGroup("Creation", Order = -100)]
    [Button]
    public void CreateInfo()
    {
        Reset();

        marchingTable = useMCode switch
        {
            true when useMCodeHeight => new List<PrototypeData>[256],
            true => new List <PrototypeData>[16], // https://ragingnexus.com/creative-code-lab/experiments/algorithms-marching-squares/
            _ => marchingTable
        };

        for (int i = 0; i < marchingTable.Length; i++)
        {
            marchingTable[i] = new List<PrototypeData>();
        }

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
                Vector3 vec = new Vector3(math.round(noDupes[g].x * divider * (2.0f / moduleScale.x)) / divider, math.round(noDupes[g].y * divider * (2.0f / moduleScale.y)) / divider, math.round(noDupes[g].z * divider * (2.0f / moduleScale.z)) / divider);

                switch (vec.x)
                {
                    case 1f:
                        posXs.Add(vec);
                        break;
                    case -1f:
                        negXs.Add(vec);
                        break;
                }
                switch (vec.y)
                {
                    case 1f:
                        posYs.Add(vec);
                        break;
                    case -1f:
                        negYs.Add(vec);
                        break;
                }
                switch (vec.z)
                {
                    case 1f:
                        posZs.Add(vec);
                        break;
                    case -1f:
                        negZs.Add(vec);
                        break;
                }
            }

            string posX = GetSideKey(posXs, 0, 1);
            string negX = GetSideKey(negXs, 0, -1);
            string posZ = GetSideKey(posZs, 1, -1);
            string negZ = GetSideKey(negZs, 1, 1);

            string[] posY = GetTopKeys(posYs, true);
            string[] negY = GetTopKeys(negYs, false);

            int[] matIndexes = meshes[i].gameObject.GetComponent<MeshRenderer>().sharedMaterials.Select((x) => materialData.Materials.IndexOf(x)).ToArray();
            // Add all rotations
            // Need to rotate the vertical too
            List<PrototypeData> prots = new List<PrototypeData>
            {
                new PrototypeData(new MeshWithRotation(mesh, 0), posX, negX, posY[0], negY[0], posZ, negZ, pieceWeights[i].Weight, matIndexes),
            };

            int copies = AddIfUnique(prots, new PrototypeData(new MeshWithRotation(mesh, 1), posZ, negZ, posY[1], negY[1], negX, posX, pieceWeights[i].Weight, matIndexes)) ? 0 : 1;
            copies += AddIfUnique(prots, new PrototypeData(new MeshWithRotation(mesh, 2), negX, posX, posY[2], negY[2], negZ, posZ, pieceWeights[i].Weight, matIndexes)) ? 0 : 1;
            copies += AddIfUnique(prots, new PrototypeData(new MeshWithRotation(mesh, 3), negZ, posZ, posY[3], negY[3], posX, negX, pieceWeights[i].Weight, matIndexes)) ? 0 : 1;

            float extraWeight = 1.0f / (1.0f - (copies / 4.0f));
            if (extraWeight > 1)
            {
                for (int j = 0; j < prots.Count; j++)
                {
                    prots[j] = new PrototypeData(prots[j].MeshRot, prots[j].PosX, prots[j].NegX, prots[j].PosY, prots[j].NegY, prots[j].PosZ, prots[j].NegZ, prots[j].Weight * extraWeight, prots[j].MaterialIndexes);
                }
            }
            Prototypes.AddRange(prots);

            if (notAllowedBottomIndexes.Contains(i))
            {
                for (int j = 1; j < prots.Count + 1; j++)
                {
                    notAllowedForBottom.Add(Prototypes.Count - j);
                }
            }
            
            if (onlyAllowedBottomIndexes.Contains(i))
            {
                for (int j = 1; j < prots.Count + 1; j++)
                {
                    onlyAllowedForBottom.Add(Prototypes.Count - j);
                }
            }

            if (NotAllowedForSides.Contains(i))
            {
                for (int j = 1; j < prots.Count + 1; j++)
                {
                    notAllowedForSides.Add(Prototypes.Count - j);
                }
            }

            if (i == castlePrototypeIndex)
            {
                castleIndex = Prototypes.Count - 1;
            }

            if (useMCode && !useMCodeHeight)
            {
                SetMarchingTable2D(prots);
            }

            if (useBuildableCorners && prots[0].Keys.Any(x => x.Contains(rightBuildableCode)))
            {
                buildableCornerData.BuildableDictionary.Add(mesh, GetBuildableCorners(prots[0]));
            }
        }

        // Empty
        PrototypeData air = new PrototypeData(new MeshWithRotation(null, 0), "-1s", "-1s", "-1s", "-1s", "-1s", "-1s", pieceWeights[meshes.Length].Weight, System.Array.Empty<int>());
        Prototypes.Add(air);

        if (useMCode && !useMCodeHeight)
        {
            marchingTable[0].Add(air);
            marchingTable[15].Add(air);
        }

        static bool AddIfUnique(List<PrototypeData> prots, PrototypeData prot)
        {
            for (int i = 0; i < prots.Count; i++)
            {
                if (prot.PosX == prots[i].PosX && prot.NegX == prots[i].NegX && prot.PosY == prots[i].PosY && prot.NegY == prots[i].NegY && prot.PosZ == prots[i].PosZ && prot.NegZ == prots[i].NegZ)
                {
                    return false;
                }
            }

            prots.Add(prot);
            return true;
        }
    }

    private BuildableCorners GetBuildableCorners(PrototypeData prot) // DOES NOT WORK FOR ALL ROTATIONS!! AAAAHH!
    {
        if (rightBuildableCode[^1] == 'f')
        {
            UnityEngine.Debug.LogError("This was not how you built it man");
        }

        bool topLeft = prot.PosZ == $"{rightBuildableCode}f";
        bool topRight = prot.PosZ == rightBuildableCode;
        bool botLeft = (prot.NegZ == "-1s" && prot.NegX == $"{rightBuildableCode}f") || prot.NegZ == $"{rightBuildableCode}";
        bool botRight = (topRight && botLeft && prot.NegZ == "-1s") || prot.NegZ == $"{rightBuildableCode}f";
        BuildableCorners corner = new BuildableCorners()
        {
            CornerDictionary = new Dictionary<Corner, bool>()
            {
                {Corner.TopLeft, topLeft}, 
                {Corner.TopRight, topRight}, 
                {Corner.BottomLeft, botLeft}, 
                {Corner.BottomRight, botRight}, 
            }
        };
        
        return corner;
    }

    private string GetSideKey(List<Vector3> vertexPositions, int mainAxis, int positiveDirection)
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
                Vector2 pro = new Vector2(vertexPositions[i].z * positiveDirection, vertexPositions[i].y);
                positions.Add(pro);

            }
            else if (mainAxis == 1)
            {
                Vector2 pro = new Vector2(vertexPositions[i].x * positiveDirection, vertexPositions[i].y);
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
            if (LooseEquals(socketList[i].positions.ToList(), positions))
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
        }

        if (LooseEquals(positions, negPositions))
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

    private string[] GetTopKeys(List<Vector3> vertexPositions, bool positive)
    {
        switch (positive)
        {
            case true when overrideVerticalPositive:
                return new[] { positiveKey, positiveKey, positiveKey, positiveKey };
            case false when overrideVerticalNegative:
                return new[] { negativeKey, negativeKey, negativeKey, negativeKey };
        }

        if ( vertexPositions.Count == 0) return new[] { "-1s", "-1s", "-1s", "-1s" };

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
            if (!LooseEquals(verticalSocketList[g].positions.ToList(), poses)) continue;
            
            string[] kes = new string[4];
            kes[0] = verticalSocketList[g].socketname;

            if (int.TryParse(kes[0][3].ToString(), out int index))
            {
                for (int i = 1; i < 4; i++)
                {
                    int newdex = index + i >= 4 ? index + i - 4 : index + i;
                    kes[i] = $"v{kes[0][1]}_{newdex}";
                }
            }

            return kes;
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
    
    private void SetMarchingTable2D(List<PrototypeData> prots)
    {
        if (Debug)
        {
            UnityEngine.Debug.Log("Mesh: " + prots[0].MeshRot.Mesh.name);
        }

        string code = prots[0].MeshRot.Mesh.name[^4..]; 
        
        for (int i = 0; i < prots.Count; i++)
        {
            int rot = prots[i].MeshRot.Rot;
            string key = code;
            if (rot > 0)
            {
                if (Debug)
                {
                    UnityEngine.Debug.Log("Rot: " + rot);
                }
                // Rotate code
                key = RotateBinaryClockwise(code, rot);
            }
            
            int index = System.Convert.ToInt32(key, 2);
            if (Debug)
            {
                UnityEngine.Debug.Log("Code: " + key);
                UnityEngine.Debug.Log("Index: " + index);
            }
            marchingTable[index].Add(prots[i]);
        }
    }
    
    private string RotateBinaryClockwise(string binaryString, int rotations)
    {
        char[] rotated = new char[4];
        for (int i = 0; i < 4; i++)
        {
            int newIndex = (i + rotations) % 4;
            rotated[newIndex] = binaryString[i];
        }

        return new string(rotated);
    }
  
    
    [TitleGroup("Display", Order = -50)]
    [Button]
    public void DisplayPrototypes()
    {
        StopDisplayingPrototypes();

        for (int i = 0; i < Prototypes.Count; i++)
        {
            Vector3 pos = new Vector3(i * 5, 0, 0);
            var prot = Instantiate(prefab, pos, Quaternion.identity);
            prot.Setup(Prototypes[i]);
            prot.GetComponentInChildren<MeshRenderer>().materials = materialData.Materials.Where(x => Prototypes[i].MaterialIndexes.Contains(materialData.Materials.IndexOf(x))).ToArray();

            spawnedPrototypes.Add(prot.gameObject);
        }
    }

    [TitleGroup("Display", Order = -50)]
    [Button]
    public void StopDisplayingPrototypes()
    {
        for (int i = 0; i < spawnedPrototypes.Count; i++)
        {
            DestroyImmediate(spawnedPrototypes[i]);
        }

        spawnedPrototypes.Clear();
    }

    [TitleGroup("Display", Order = -50)]
    [Button]
    public void Clear()
    {
        Reset();
    }

    [TitleGroup("Util", Order = -45)]
    [Button]
    public void CreateWeights()
    {
        pieceWeights = new List<StupidWeightThing>();

        for (int i = 0; i < transform.childCount; i++)
        {
            pieceWeights.Add(new StupidWeightThing(transform.GetChild(i).name, 1));
        }

        pieceWeights.Add(new StupidWeightThing("Air", 1));
    }

    private void Reset()
    {
        Prototypes.Clear();
        currentSideIndex = 0;
        currentTopIndex = 0;
        socketList.Clear();
        verticalSocketList.Clear();
        NotAllowedForBottom.Clear();
        OnlyAllowedForBottom.Clear();
        NotAllowedForSides.Clear();
        buildableCornerData?.Clear();
        marchingTable = Array.Empty<List<PrototypeData>>();
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

    public bool LooseEquals(List<Vector2> vec1, List<Vector2> vec2)
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

    private List<Vector2> Rounded(List<Vector2> vec)
    {
        List<Vector2> rounded = new List<Vector2>();
        for (int i = 0; i < vec.Count; i++)
        {
            rounded.Add(new Vector2(math.round(vec[i].x * divider) / divider, math.round(vec[i].y * divider) / divider));
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
            float y = positions[i].x * Mathf.Sin(angle) + positions[i].y * Mathf.Cos(angle);
            rotated[i] = new Vector2(x, y);
        }

        return Rounded(rotated.ToList()).ToArray();
    }

    [TitleGroup("Misc", Order = -40)]
    [Button]
    public void SpaceChildren()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).transform.position = new Vector3(i * 2, 0, 0);
        }
    }

    [TitleGroup("Misc", Order = -40)]
    [Button]
    public void PrintChildrenCount()
    {
        UnityEngine.Debug.Log(transform.childCount);
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
public struct PrototypeData : System.IEquatable<PrototypeData>
{
    public MeshWithRotation MeshRot;

    public string PosX;
    public string NegX; 
    public string PosZ;
    public string NegZ;
    public string PosY;
    public string NegY;
    public float Weight;

    public int[] MaterialIndexes;

    public readonly string[] Keys => new string[6] 
    {
        PosX, NegX, PosY, NegY, PosZ, NegZ
    };

    public PrototypeData(MeshWithRotation mesh, string posX, string negX, string posY, string negY, string posZ, string negZ, float weight, int[] mats)
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

    public static bool operator ==(PrototypeData p1, PrototypeData p2)
    {
        return p1.Equals(p2);
    }

    public static bool operator !=(PrototypeData p1, PrototypeData p2)
    {
        return !p1.Equals(p2);
    }

    public readonly override bool Equals(object obj)
    {
        return obj is PrototypeData data && Equals(data);
    }

    public readonly override int GetHashCode()
    {
        return System.HashCode.Combine(PosX, NegX, PosZ, NegZ, PosY, NegY, Weight);
    }

    public readonly bool Equals(PrototypeData data)
    {
        return PosX == data.PosX &&
               NegX == data.NegX &&
               PosZ == data.PosZ &&
               NegZ == data.NegZ &&
               PosY == data.PosY &&
               NegY == data.NegY &&
               Mathf.Approximately(Weight, data.Weight) &&
               MeshRot.Mesh == data.MeshRot.Mesh;
    }
    
    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("PrototypeData { ");
        sb.Append("MeshRot: { Mesh: ").Append(MeshRot.Mesh != null ? MeshRot.Mesh.name : "null").Append(", Rotation: ").Append(MeshRot.Rot).Append(" }, ");
        sb.Append("PosX: ").Append(PosX).Append(", NegX: ").Append(NegX).Append(", ");
        sb.Append("PosY: ").Append(PosY).Append(", NegY: ").Append(NegY).Append(", ");
        sb.Append("PosZ: ").Append(PosZ).Append(", NegZ: ").Append(NegZ).Append(", ");
        sb.Append("Weight: ").Append(Weight);
        sb.Append("}"); 
        return sb.ToString();
    }
}

public static class ListExtensions
{
    public static bool LooseEquals<T>(this List<T> vec1, List<T> vec2)
    {
        if (vec1.Count != vec2.Count)
        {
            return false;
        }

        for (int i = 0; i < vec2.Count; i++)
        {
            if (!vec1.Contains(vec2[i]))
            {
                return false;
            }
        }

        return true;
    }
}

[System.Serializable]
public struct StupidWeightThing
{
    public string Name;
    public float Weight;

    public StupidWeightThing(string name, float weight)
    {
        Name = name;
        Weight = weight;
    }
}