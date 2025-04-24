using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace WaveFunctionCollapse
{
    [CreateAssetMenu(fileName = "New ProtoypeMeshes", menuName = "WFC/ProtoypeMeshes", order = 0)]
    [InlineEditor]
    public class ProtoypeMeshes : SerializedScriptableObject
    {
        [Title("Meshes"), ReadOnly]
        public readonly Dictionary<int, Mesh> Meshes = new Dictionary<int, Mesh>();

        [Title("References")]
        [SerializeField]
        private PrototypeInfoData[] protoypeInfos;
        
        #if UNITY_EDITOR
        [SerializeField]
        private PrototypeInfoCreator[] protoypeInfoCreators;
        #endif
        
        public Mesh this[int meshRotMeshIndex] => Meshes[meshRotMeshIndex];

        #if UNITY_EDITOR
        [Button]
        public void CompileData()
        {
            Meshes.Clear();
            Dictionary<Mesh, int> meshesToIndex = new Dictionary<Mesh, int>();
            int index = 0;
            for (int i = 0; i < protoypeInfos.Length; i++)
            {
                for (int j = 0; j < protoypeInfos[i].PrototypeMeshes.Count; j++)
                {
                    if (!Meshes.TryAdd(index, protoypeInfos[i].PrototypeMeshes[j])) continue;
                    
                    meshesToIndex.Add(protoypeInfos[i].PrototypeMeshes[j], index);
                    index++;
                }

                for (int j = 0; j < protoypeInfos[i].Prototypes.Count; j++)
                {
                    PrototypeData prot = protoypeInfos[i].Prototypes[j];
                    if (prot.MeshRot.MeshIndex == -1) continue;

                    if (prot.MeshRot.MeshIndex >= protoypeInfos[i].PrototypeMeshes.Count) continue;
                    
                    int meshIndex = meshesToIndex[protoypeInfos[i].PrototypeMeshes[prot.MeshRot.MeshIndex]];
                    PrototypeData prototypeData = new PrototypeData(
                        new MeshWithRotation(meshIndex, prot.MeshRot.Rot),
                        prot.PosX, prot.NegX, prot.PosY, prot.NegY, prot.PosZ, prot.NegZ,
                        prot.Weight, prot.MaterialIndexes);
                    prototypeData.Name_EditorOnly = Meshes[meshIndex].name;
                        
                    protoypeInfos[i].Prototypes[j] = prototypeData;
                        
                }
                
                for (int j = 0; j < protoypeInfos[i].MarchingTable.Length; j++)
                {
                    for (int k = 0; k < protoypeInfos[i].MarchingTable[j].Count; k++)
                    {
                        PrototypeData prot = protoypeInfos[i].MarchingTable[j][k];
                        if (prot.MeshRot.MeshIndex == -1) continue;
                        if (prot.MeshRot.MeshIndex >= protoypeInfos[i].PrototypeMeshes.Count) continue;

                        int meshIndex = meshesToIndex[protoypeInfos[i].PrototypeMeshes[prot.MeshRot.MeshIndex]];
                        PrototypeData prototypeData = new PrototypeData(
                            new MeshWithRotation(meshIndex, prot.MeshRot.Rot),
                            prot.PosX, prot.NegX, prot.PosY, prot.NegY, prot.PosZ, prot.NegZ,
                            prot.Weight, prot.MaterialIndexes);
                        prototypeData.Name_EditorOnly = Meshes[meshIndex].name;
                        protoypeInfos[i].MarchingTable[j][k] = prototypeData;
                    }
                }
                
                protoypeInfos[i].NotBottomPrototypes.Clear();
                
                UnityEditor.EditorUtility.SetDirty(protoypeInfos[i]);
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
        }

        [Button]
        public void CompilePrototypes()
        {
            for (int i = 0; i < protoypeInfoCreators.Length; i++)
            {
                protoypeInfoCreators[i].CreateInfo();
            }
        }
        #endif
    }
}