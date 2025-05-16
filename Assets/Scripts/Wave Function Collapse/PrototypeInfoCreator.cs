using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using System.Linq;
using Sirenix.Utilities;

namespace WaveFunctionCollapse
{
#if UNITY_EDITOR
    public class PrototypeInfoCreator : SerializedMonoBehaviour
    {
        [Title("Debug")]
        public bool Debug;

        [SerializeField]
        private PrototypeDisplay prefab;

        [SerializeField]
        private float divider = 100f;

        [Title("Rules")]
        [SerializeField]
        private List<int> notAllowedBottomIndexes;
        
        [SerializeField]
        private List<int> onlyAllowedBottomIndexes;

        [SerializeField]
        private List<int> notAllowedSideIndexes;

        [Title("Settings")]
        [SerializeField]
        private Vector3 moduleScale;

        [SerializeField]
        private bool useVerticiesOutsideUnitCube = false;
        
        [SerializeField]
        private bool useMCode = false;

        [SerializeField]
        [ShowIf(nameof(useMCode))]
        private bool useMCodeHeight = false;
        
        [SerializeField]
        private bool useBuildableCorners = false;

        [SerializeField, ShowIf(nameof(useBuildableCorners))]
        private short rightBuildableCode = 0;
        
        [SerializeField, ShowIf(nameof(useBuildableCorners))]
        private BuildableCornerData buildableCornerData;

        [Title("Generated")]
        [SerializeField]
        private PrototypeInfoData prototypeData;
        
        [Title("Material")]
        [SerializeField]
        private MaterialData materialData;

        [TitleGroup("Weight")]
        [SerializeField]
        private List<StupidWeightThing> pieceWeights;

        private List<GameObject> spawnedPrototypes = new List<GameObject>();

        private short currentSideIndex = 0;
        private short currentTopIndex = 0;

        [TitleGroup("Creation", Order = -100)]
        [Button]
        public void CreateInfo()
        {
            Reset();

            currentSideIndex = 0;
            currentTopIndex = 0;

            prototypeData.MarchingTable = useMCode switch
            {
                true when useMCodeHeight => new List<PrototypeData>[256],
                true => new List <PrototypeData>[16], // https://ragingnexus.com/creative-code-lab/experiments/algorithms-marching-squares/
                _ => prototypeData.MarchingTable
            };

            for (int i = 0; i < prototypeData.MarchingTable.Length; i++)
            {
                prototypeData.MarchingTable[i] = new List<PrototypeData>();
            }

            MeshFilter[] meshes = GetComponentsInChildren<MeshFilter>();
            prototypeData.PrototypeMeshes = new List<Mesh>(); 
            for (int i = 0; i < meshes.Length; i++)
            {
                Mesh mesh = meshes[i].sharedMesh;
                prototypeData.PrototypeMeshes.Add(mesh);
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

                    if (useVerticiesOutsideUnitCube)
                    {
                        GetSideKeys(vec, posXs, negXs, posYs, negYs, posZs, negZs);
                    }
                    else
                    {
                        GetSideKeys_OnlyInside(vec, posXs, negXs, posYs, negYs, posZs, negZs);
                    }
                }

                short posX = GetSideKey(posXs, 0, 1);
                short negX = GetSideKey(negXs, 0, -1);
                short posZ = GetSideKey(posZs, 1, -1);
                short negZ = GetSideKey(negZs, 1, 1);

                short[] posY = GetTopKeys(posYs);
                short[] negY = GetTopKeys(negYs);

                int[] matIndexes = meshes[i].gameObject.GetComponent<MeshRenderer>().sharedMaterials.Select((x) => materialData.Materials.IndexOf(x)).ToArray();
                // Add all rotations
                // Need to rotate the vertical too
                List<PrototypeData> prots = new List<PrototypeData>
                {
                    new PrototypeData(new MeshWithRotation(i, 0), posX, negX, posY[0], negY[0], posZ, negZ, pieceWeights[i].Weight, matIndexes),
                };

                int copies = AddIfUnique(prots, new PrototypeData(new MeshWithRotation(i, 1), posZ, negZ, posY[1], negY[1], negX, posX, pieceWeights[i].Weight, matIndexes)) ? 0 : 1;
                copies += AddIfUnique(prots, new PrototypeData(new MeshWithRotation(i, 2), negX, posX, posY[2], negY[2], negZ, posZ, pieceWeights[i].Weight, matIndexes)) ? 0 : 1;
                copies += AddIfUnique(prots, new PrototypeData(new MeshWithRotation(i, 3), negZ, posZ, posY[3], negY[3], posX, negX, pieceWeights[i].Weight, matIndexes)) ? 0 : 1;

                float extraWeight = 1.0f / (1.0f - (copies / 4.0f));
                if (extraWeight > 1)
                {
                    for (int j = 0; j < prots.Count; j++)
                    {
                        prots[j] = new PrototypeData(prots[j].MeshRot, prots[j].PosX, prots[j].NegX, prots[j].PosY, prots[j].NegY, prots[j].PosZ, prots[j].NegZ, prots[j].Weight * extraWeight, prots[j].MaterialIndexes);
                    }
                }
                prototypeData.Prototypes.AddRange(prots);

                if (notAllowedBottomIndexes.Contains(i))
                {
                    for (int j = 1; j < prots.Count + 1; j++)
                    {
                        prototypeData.NotAllowedForBottom.Add(prototypeData.Prototypes.Count - j);
                    }
                }
                
                if (onlyAllowedBottomIndexes.Contains(i))
                {
                    for (int j = 1; j < prots.Count + 1; j++)
                    {
                        prototypeData.OnlyAllowedForBottom.Add(prototypeData.Prototypes.Count - j);
                    }
                }

                if (prototypeData.NotAllowedForSides.Contains(i))
                {
                    for (int j = 1; j < prots.Count + 1; j++)
                    {
                        prototypeData.NotAllowedForSides.Add(prototypeData.Prototypes.Count - j);
                    }
                }

                if (useMCode && !useMCodeHeight)
                {
                    SetMarchingTable2D(prots);
                }

                if (useBuildableCorners && prots[0].Keys.Any(x => x % 1000 == rightBuildableCode))
                {
                    buildableCornerData.BuildableDictionary.Add(mesh, GetBuildableCorners(prots[0]));
                }
            }

            // Empty
            PrototypeData air = new PrototypeData(new MeshWithRotation(-1, 0), -1, -1, -1, -1, -1, -1, pieceWeights[meshes.Length].Weight, System.Array.Empty<int>());
            prototypeData.Prototypes.Add(air);

            if (useMCode && !useMCodeHeight)
            {
                prototypeData.MarchingTable[0].Add(air);
                prototypeData.MarchingTable[15].Add(air);
            }

            if (useBuildableCorners)
            {
                buildableCornerData.Save();
            }
            
            prototypeData.Save();

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

        private static void GetSideKeys_OnlyInside(Vector3 vec, List<Vector3> posXs, List<Vector3> negXs, List<Vector3> posYs, List<Vector3> negYs, List<Vector3> posZs, List<Vector3> negZs)
        {
            switch (vec.x)
            {
                case 1f when vec is { y: <= 1.0f and >= -1.0f, z: <= 1.0f and >= -1.0f }:
                    posXs.Add(vec);
                    break;
                case -1f when vec is { y: <= 1.0f and >= -1.0f, z: <= 1.0f and >= -1.0f }:
                    negXs.Add(vec);
                    break;
            }
            switch (vec.y)
            {
                case 1f when vec is { x: <= 1.0f and >= -1.0f, z: <= 1.0f and >= -1.0f }:
                    posYs.Add(vec);
                    break;
                case -1f when vec is { x: <= 1.0f and >= -1.0f, z: <= 1.0f and >= -1.0f }:
                    negYs.Add(vec);
                    break;
            }
            switch (vec.z)
            {
                case 1f when vec is { y: <= 1.0f and >= -1.0f, x: <= 1.0f and >= -1.0f }:
                    posZs.Add(vec);
                    break;
                case -1f when vec is { y: <= 1.0f and >= -1.0f, x: <= 1.0f and >= -1.0f }:
                    negZs.Add(vec);
                    break;
            }
        }
        
        private static void GetSideKeys(Vector3 vec, List<Vector3> posXs, List<Vector3> negXs, List<Vector3> posYs, List<Vector3> negYs, List<Vector3> posZs, List<Vector3> negZs)
        {
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

        private BuildableCorners GetBuildableCorners(PrototypeData prot) // DOES NOT WORK FOR ALL ROTATIONS!! AAAAHH!
        {
            bool topLeft = prot.PosZ == rightBuildableCode + 1000;
            bool topRight = prot.PosZ == rightBuildableCode;
            bool botLeft = (prot.NegZ == -1 && prot.NegX == rightBuildableCode + 1000) || prot.NegZ == rightBuildableCode;
            bool botRight = (topRight && botLeft && prot.NegZ == -1) || prot.NegZ == rightBuildableCode + 1000;
            BuildableCorners corner = new BuildableCorners()
            {
                CornerDictionary = new Dictionary<Corner, CornerData>()
                {
                    {Corner.TopLeft, topLeft}, 
                    {Corner.TopRight, topRight}, 
                    {Corner.BottomLeft, botLeft}, 
                    {Corner.BottomRight, botRight}, 
                }
            };
            
            return corner;
        }

        private short GetSideKey(List<Vector3> vertexPositions, int mainAxis, int positiveDirection)
        {
            if (vertexPositions.Count == 0)
            {
                return -1;
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
            for (int i = 0; i < prototypeData.SocketList.Count; i++)
            {
                if (StrictEquals(prototypeData.SocketList[i].positions.ToList(), positions))
                {
                    return prototypeData.SocketList[i].socketname;
                }
            }

            for (int i = 0; i < prototypeData.SocketList.Count; i++)
            {
                if (LooseEquals(prototypeData.SocketList[i].positions.ToList(), positions))
                {
                    return prototypeData.SocketList[i].socketname;
                }
            }

            short key = currentSideIndex++;

            // Check for symmetry
            List<Vector2> negPositions = new List<Vector2>();
            for (int h = 0; h < positions.Count; h++)
            {
                negPositions.Add(new Vector2((2.0f - (positions[h].x + 1) - 1), positions[h].y));
            }

            if (LooseEquals(positions, negPositions))
            {
                key += 2000;
            }
            else
            {
                prototypeData.SocketList.Add(new DicData(negPositions.ToArray(), (short)(key + 1000)));
            }

            prototypeData.SocketList.Add(new DicData(positions.ToArray(), key));

            return key;
        }

        private short[] GetTopKeys(List<Vector3> vertexPositions)
        {
            if ( vertexPositions.Count == 0) return new short[] { -1, -1, -1, -1 };

            // Project on 2 Dimensional plane
            List<Vector2> positions = new List<Vector2>();
            for (int i = 0; i < vertexPositions.Count; i++)
            {
                positions.Add(new Vector2(vertexPositions[i].x, vertexPositions[i].z));
            }

            // Check if already in dictionary
            // Check only the first one then copy for the rest
            List<Vector2> poses = Rounded(positions);

            for (int g = 0; g < prototypeData.VerticalSocketList.Count; g++)
            {
                if (!LooseEquals(prototypeData.VerticalSocketList[g].positions.ToList(), poses)) continue;
                
                short[] existingKeys = new short[4];
                existingKeys[0] = prototypeData.VerticalSocketList[g].socketname;
                short index = Math.GetSecondSocketValue(existingKeys[0]);
                short keyValue = (short)(existingKeys[0] % 100);
                
                for (int i = 1; i < 4; i++)
                {
                    int newdex = (index + i) % 4;
                    existingKeys[i] = (short)(5000 + newdex * 100 +  keyValue);
                }

                return existingKeys;
            }

            // Add all four rotations
            short[] keys = new short[4];
            for (int i = 0; i < 4; i++)
            {
                short key = (short)(5000 + i * 100 + currentTopIndex);
                Vector2[] pos = Rotated(positions, i);
                keys[i] = key;
                prototypeData.VerticalSocketList.Add(new DicData(pos, key));
            }

            currentTopIndex++;

            return keys;
        }
        
        private void SetMarchingTable2D(List<PrototypeData> prots)
        {
            if (Debug)
            {
                UnityEngine.Debug.Log("Mesh: " + prototypeData.PrototypeMeshes[prots[0].MeshRot.MeshIndex].name);
            }

            string code =  prototypeData.PrototypeMeshes[prots[0].MeshRot.MeshIndex].name[^4..]; 
            
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
                prototypeData.MarchingTable[index].Add(prots[i]);
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

            for (int i = 0; i < prototypeData.Prototypes.Count; i++)
            {
                Vector3 pos = new Vector3(i * 5, 0, 0);
                var prot = Instantiate(prefab, pos, Quaternion.identity);
                prot.Setup(prototypeData.Prototypes[i]);
                prot.GetComponentInChildren<MeshRenderer>().materials = materialData.Materials.Where(x => prototypeData.Prototypes[i].MaterialIndexes.Contains(materialData.Materials.IndexOf(x))).ToArray();

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

        [TitleGroup("Weight")]
        [Button]
        public void CreateWeights(bool resetValues = false)
        {
            var oldWeights = new List<StupidWeightThing>(pieceWeights);
            pieceWeights = new List<StupidWeightThing>();

            for (int i = 0; i < transform.childCount; i++)
            {
                pieceWeights.Add(new StupidWeightThing(transform.GetChild(i).name, 1));
            }
            pieceWeights.Add(new StupidWeightThing("Air", 1));

            if (resetValues) return;
            
            for (int i = 0; i < pieceWeights.Count; i++)
            {
                for (int j = 0; j < oldWeights.Count; j++)
                {
                    if (string.Equals(pieceWeights[i].Name, oldWeights[j].Name))
                    {
                        pieceWeights[i] = oldWeights[j];
                    }   
                }
                
            }
        }

        [TitleGroup("Weight")]
        [Button]
        public void ModifyWeights(string withAll, float multiplier = 2)
        {
            for (int i = 0; i < pieceWeights.Count; i++)
            {
                if (!pieceWeights[i].Name.Contains(withAll)) continue;
                
                var weight = pieceWeights[i];
                weight.Weight *= multiplier;
                pieceWeights[i] = weight;
            }
        }
        
        private void Reset()
        {
            currentSideIndex = 0;
            currentTopIndex = 0;
            prototypeData.Clear();
            buildableCornerData?.Clear();
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
#endif
    [System.Serializable]
    public struct DicData
    {
        public Vector2[] positions;
        public short socketname;

        public DicData(Vector2[] positions, short socketname)
        {
            this.positions = positions;
            this.socketname = socketname;
        }
    }

    [System.Serializable]
    public struct PrototypeData : System.IEquatable<PrototypeData>
    {
#if UNITY_EDITOR
        [ReadOnly]
        public string Name_EditorOnly;
#endif
        
        public MeshWithRotation MeshRot;

        public short PosX;
        public short NegX;
        public short PosZ;
        public short NegZ;
        public short PosY;
        public short NegY;
        public float Weight;

        public int[] MaterialIndexes;

        public readonly short[] Keys => new short[6] 
        {
            PosX, NegX, PosY, NegY, PosZ, NegZ
        };

        public static PrototypeData Empty { get; set; } = new PrototypeData(new MeshWithRotation(-1, 0), -1, -1, -1, -1, -1, -1, 1, Array.Empty<int>());

        public readonly short DirectionToKey(Direction direction) => direction switch 
        {
            Direction.Right => PosX,
            Direction.Left => NegX,
            Direction.Up => PosY,
            Direction.Down => NegY,
            Direction.Forward => PosZ,
            Direction.Backward => NegZ,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        }; 

        public PrototypeData(MeshWithRotation mesh, short posX, short negX, short posY, short negY, short posZ, short negZ, float weight, int[] mats)
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
#if UNITY_EDITOR
            Name_EditorOnly = "";
#endif
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
                   MeshRot.MeshIndex == data.MeshRot.MeshIndex;
        }
        
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("PrototypeData { ");
            sb.Append("MeshRot: { Mesh: ").Append(MeshRot.MeshIndex).Append(", Rotation: ").Append(MeshRot.Rot).Append(" }, ");
            //sb.Append("PosX: ").Append(PosX).Append(", NegX: ").Append(NegX).Append(", ");
            //sb.Append("PosY: ").Append(PosY).Append(", NegY: ").Append(NegY).Append(", ");
            //sb.Append("PosZ: ").Append(PosZ).Append(", NegZ: ").Append(NegZ).Append(", ");
            //sb.Append("Weight: ").Append(Weight);
            sb.Append("}"); 
            return sb.ToString();
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
}
