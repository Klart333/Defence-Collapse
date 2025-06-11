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

            posX.text = KeyIndexToLabel(prototype.PosX);
            negX.text = KeyIndexToLabel(prototype.NegX);
            posY.text = KeyIndexToLabel(prototype.PosY);
            negY.text = KeyIndexToLabel(prototype.NegY);
            posZ.text = KeyIndexToLabel(prototype.PosZ);
            negZ.text = KeyIndexToLabel(prototype.NegZ);
        }

        private string KeyIndexToLabel(ulong key)
        {
            int index = (int)Math.Log(key, 2);
            
            return index switch
            {
                <= 32 => $"{index:N0}s",
                <= 48 => $"{index - 32:N0}",
                <= 64 => $"{index - 48:N0}f",
                _ => $"bit{index}"
            };
        }
    }
}

