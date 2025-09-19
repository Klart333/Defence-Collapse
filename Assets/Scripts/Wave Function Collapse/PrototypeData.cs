using Sirenix.OdinInspector;
using System.Text;
using UnityEngine;
using System;

namespace WaveFunctionCollapse
{
    [Serializable]
    public struct PrototypeData : IEquatable<PrototypeData>
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
            return HashCode.Combine(PosX, NegX, PosZ, NegZ, PosY, NegY, Weight);
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
            var sb = new StringBuilder();
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


    public static class PrototypeDataUtility
    {
        public static bool IsKeySymmetrical(ulong key)
        {
            int value = (int)Math.Log(key, 2);
            return value <= 32;
        }
    }
}