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

        public void Setup(PrototypeData prototype)
        {
            mesh.GetComponent<MeshFilter>().mesh = prototype.MeshRot.Mesh;
            mesh.transform.rotation = Quaternion.Euler(0, 90 * prototype.MeshRot.Rot, 0);

            posX.text = prototype.PosX;
            negX.text = prototype.NegX;
            posY.text = prototype.PosY;
            negY.text = prototype.NegY;
            posZ.text = prototype.PosZ;
            negZ.text = prototype.NegZ;
        }
    }
}

