using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using System.Linq;
using System;

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
        private List<int> onlyAllowedBottomIndexes;

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
        private List<ulong> rightBuildableCodes;
        
        [SerializeField, ShowIf(nameof(useBuildableCorners))]
        private BuildableCornerData buildableCornerData;

        [Title("Generated")]
        [SerializeField]
        private PrototypeInfoData prototypeData;
        
        [Title("Material")]
        [SerializeField]
        private MaterialData materialData;

        [SerializeField]
        private bool useMaterialForKeys;
        
        [SerializeField, ShowIf(nameof(useMaterialForKeys))]
        private IMeshRayService meshRayService;
        
        [TitleGroup("Weight")]
        [SerializeField]
        private List<StupidWeightThing> pieceWeights;

        private List<GameObject> spawnedPrototypes = new List<GameObject>();

        private int currentSideSymmetricalIndex = 0;
        private int currentSideIndex = 0;
        private short currentTopIndex = 0;

        [TitleGroup("Creation", Order = -100)]
        [Button]
        public void CreateInfo()
        {
            Reset();

            currentSideSymmetricalIndex = 0;
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
                Material[] materials = meshes[i].gameObject.GetComponent<MeshRenderer>().sharedMaterials;
                int[] matIndexes = materials.Select((x) => materialData.Materials.IndexOf(x)).ToArray();

                Dictionary<Direction, int[]> materialInfo = useMaterialForKeys ? meshRayService.GetMeshIndices(mesh, materials) : null;
                
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

                ulong posX = GetSideKey(posXs, Direction.Right, materialInfo);
                ulong negX = GetSideKey(negXs, Direction.Left, materialInfo);
                ulong posZ = GetSideKey(posZs, Direction.Forward, materialInfo);
                ulong negZ = GetSideKey(negZs, Direction.Backward, materialInfo);

                ulong[] negY = GetTopKeys(negYs);
                ulong[] posY = GetTopKeys(posYs);

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

                if (useBuildableCorners && prots[0].Keys.Any(x => rightBuildableCodes.Contains(x)))
                {
                    buildableCornerData.BuildableDictionary.Add(mesh, GetBuildableCorners(prots[0]));
                }
            }

            // Empty
            PrototypeData air = new PrototypeData(new MeshWithRotation(-1, 0), 1, pieceWeights[meshes.Length].Weight, System.Array.Empty<int>());
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

        private BuildableCorners GetBuildableCorners(PrototypeData prot)
        {
            bool topLeft = rightBuildableCodes.Contains(prot.NegX);
            bool topRight = rightBuildableCodes.Contains(prot.PosZ);
            bool botLeft = rightBuildableCodes.Contains(prot.NegZ);
            bool botRight = rightBuildableCodes.Contains(prot.PosX);
            
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

        private ulong GetSideKey(List<Vector3> vertexPositions, Direction direction, Dictionary<Direction, int[]> materialInfo)
        {
            if (vertexPositions.Count == 0)
            {
                return 1;
            }
            
            int[] mats = useMaterialForKeys ? materialInfo[direction] : Array.Empty<int>();

            // Project on 2 Dimensional plane
            List<Vector2> positions = new List<Vector2>();
            for (int i = 0; i < vertexPositions.Count; i++)
            {
                positions.Add(direction switch
                {
                    Direction.Right => new Vector2(vertexPositions[i].z, vertexPositions[i].y),
                    Direction.Left => new Vector2(vertexPositions[i].z * -1, vertexPositions[i].y),
                    Direction.Forward => new Vector2(vertexPositions[i].x * -1, vertexPositions[i].y),
                    Direction.Backward => new Vector2(vertexPositions[i].x, vertexPositions[i].y),
                    _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
                });
            }

            // Check if already in dictionary
            // First check the strict equals
            for (int i = 0; i < prototypeData.SocketList.Count; i++)
            {
                if (StrictEquals(prototypeData.SocketList[i].positions.ToList(), positions)
                    && (!useMaterialForKeys || StrictEquals(prototypeData.SocketList[i].materialIndexes, mats)))
                {
                    return prototypeData.SocketList[i].socketname;
                }
            }

            for (int i = 0; i < prototypeData.SocketList.Count; i++)
            {
                if (LooseEquals(prototypeData.SocketList[i].positions.ToList(), positions)
                    && (!useMaterialForKeys || StrictEquals(prototypeData.SocketList[i].materialIndexes, mats)))
                {
                    return prototypeData.SocketList[i].socketname;
                }
            }

            ulong key = 0;
            // Check for symmetry
            List<Vector2> negPositions = new List<Vector2>();
            for (int h = 0; h < positions.Count; h++)
            {
                negPositions.Add(new Vector2((2.0f - (positions[h].x + 1) - 1), positions[h].y));
            }

            if (LooseEquals(positions, negPositions) && mats.Length == 1)
            {
                key = (ulong)1 << (2 + currentSideSymmetricalIndex++);
                prototypeData.SocketList.Add(new DicData(positions.ToArray(), key, mats));
            }
            else
            {
                key = (ulong)1 << (33 + currentSideIndex++);
                prototypeData.SocketList.Add(new DicData(positions.ToArray(), key, mats));
                prototypeData.SocketList.Add(new DicData(negPositions.ToArray(), key << 16, mats.Reverse().ToArray()));
            }

            return key;
        }

        private ulong[] GetTopKeys(List<Vector3> vertexPositions)
        {
            if ( vertexPositions.Count == 0) return new ulong[]
            {
                1, 1, 1, 1
            };

            // Project on 2 Dimensional plane
            List<Vector2> positions = new List<Vector2>();
            for (int i = 0; i < vertexPositions.Count; i++)
            {
                positions.Add(new Vector2(vertexPositions[i].x, vertexPositions[i].z));
            }

            // Check if already in dictionary
            // Check only the first one then copy for the rest
            List<Vector2> poses = Rounded(positions);

            if (TryMatchExistingVerticalKeys(poses, out ulong[] keys))
            {
                return keys;
            }

            bool TryMatchExistingVerticalKeys(List<Vector2> positionList, out ulong[] keys)
            {
                keys = new ulong[4];
                bool4 valid = new bool4();

                for (int i = 0; i < 4; i++)
                {
                    Vector2[] pos = Rotated(positionList, i);

                    for (int g = 0; g < prototypeData.VerticalSocketList.Count; g++)
                    {
                        if (!LooseEquals(prototypeData.VerticalSocketList[g].positions, pos)) continue;
                
                        keys[i] = prototypeData.VerticalSocketList[g].socketname;
                        valid[i] = true;
                        break;
                    }
                }

                return math.all(valid);
            }

            // Add all four rotations
            for (int i = 0; i < 4; i++)
            {
                ulong key = ((ulong)1 << (currentTopIndex + 2 + i));
                Vector2[] pos = Rotated(positions, i);
                keys[i] = key;
                prototypeData.VerticalSocketList.Add(new DicData(pos, key, Array.Empty<int>()));
            }

            currentTopIndex += 4;

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
                PrototypeDisplay prot = Instantiate(prefab, pos, Quaternion.identity);
                prot.Setup(prototypeData.Prototypes[i]);
                if (!prot.UseMaterials)
                {
                    prot.GetComponentInChildren<MeshRenderer>().materials = materialData.Materials.Where(x => prototypeData.Prototypes[i].MaterialIndexes.Contains(materialData.Materials.IndexOf(x))).ToArray();
                }

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
        
        private bool StrictEquals(List<Vector2> vec1, List<Vector2> vec2)
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
        
        private bool StrictEquals(int[] arr1, int[] arr2)
        {
            if (arr1.Length != arr2.Length)
            {
                return false;
            }

            for (int i = 0; i < arr2.Length; i++)
            {
                if (arr1[i] != arr2[i])
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
        
        public bool LooseEquals(Vector2[] vec1, Vector2[] vec2)
        {
            if (vec1.Length != vec2.Length)
            {
                return false;
            }

            vec1 = Rounded(vec1);
            vec2 = Rounded(vec2);

            for (int i = 0; i < vec2.Length; i++)
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
        
        private Vector2[] Rounded(Vector2[] vec)
        {
            Vector2[] rounded = new Vector2[vec.Length];
            for (int i = 0; i < vec.Length; i++)
            {
                rounded[i] = (new Vector2(math.round(vec[i].x * divider) / divider, math.round(vec[i].y * divider) / divider));
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

    public interface IMeshRayService
    {
        public Dictionary<Direction, int[]> GetMeshIndices(Mesh mesh, Material[] mats);
    }
    
#endif
    [System.Serializable]
    public struct DicData
    {
        public Vector2[] positions;
        public ulong socketname;
        public int[] materialIndexes;

        public DicData(Vector2[] positions, ulong socketname, int[] materialIndexes)
        {
            this.positions = positions;
            this.socketname = socketname;
            this.materialIndexes = materialIndexes;
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

        public ulong PosX;
        public ulong NegX;
        public ulong PosZ;
        public ulong NegZ;
        public ulong PosY;
        public ulong NegY;
        public float Weight;

        public int[] MaterialIndexes;

        public readonly ulong[] Keys => new ulong[6] 
        {
            PosX, NegX, PosY, NegY, PosZ, NegZ
        };

        public static PrototypeData Empty { get; set; } = new PrototypeData(new MeshWithRotation(-1, 0), 1, 1, Array.Empty<int>());

        public readonly ulong DirectionToKey(Direction direction) => Keys[(int)direction]; 

        public PrototypeData(MeshWithRotation mesh, ulong posX, ulong negX, ulong posY, ulong negY, ulong posZ, ulong negZ, float weight, int[] mats)
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
        
        public PrototypeData(MeshWithRotation mesh, ulong allKeys, float weight, int[] mats)
        {
            MaterialIndexes = mats;
            MeshRot = mesh;
            PosX = allKeys;
            NegX = allKeys;
            PosY = allKeys;
            NegY = allKeys;
            PosZ = allKeys;
            NegZ = allKeys;

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
