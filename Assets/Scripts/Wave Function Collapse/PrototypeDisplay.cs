using System;
using TMPro;
using UnityEngine;

namespace WaveFunctionCollapse
{
    public class PrototypeDisplay : MonoBehaviour
    {
        [SerializeField]
        private GameObject mesh;

        [SerializeField]
        private TextMeshPro posX;
        [SerializeField]
        private TextMeshPro negX;
        [SerializeField]
        private TextMeshPro posY;
        [SerializeField]
        private TextMeshPro negY;
        [SerializeField]
        private TextMeshPro posZ;
        [SerializeField]
        private TextMeshPro negZ;

        [SerializeField]
        private ProtoypeMeshes protoypeMeshes;
        
        public void Setup(PrototypeData prototype)
        {
            if (prototype.MeshRot.MeshIndex != -1)
            {
                mesh.GetComponent<MeshFilter>().mesh = protoypeMeshes[prototype.MeshRot.MeshIndex];
            }
            mesh.transform.rotation = Quaternion.Euler(0, 90 * prototype.MeshRot.Rot, 0);

            posX.text = GetString(prototype.PosX);
            negX.text = GetString(prototype.NegX);
            posY.text = GetString(prototype.PosY);
            negY.text = GetString(prototype.NegY);
            posZ.text = GetString(prototype.PosZ);
            negZ.text = GetString(prototype.NegZ);
        }

        private string GetString(short key)
        {
            return key switch
            {
                >= 5000 => $"v{key % 100}_{Utility.Math.GetSecondSocketValue(key)}",
                >= 2000 => $"{key - 2000}s",
                >= 1000 => $"{key - 1000}f", 
                _ => key.ToString()
            };
        }
    }
}

