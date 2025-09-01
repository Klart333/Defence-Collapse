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
        
        public Mesh this[int meshIndex] => Meshes[meshIndex];

        #if UNITY_EDITOR
        [Button]
        public void CompileData()
        {
            Meshes.Clear();
            
            Dictionary<Mesh, int> meshesToIndex = new Dictionary<Mesh, int>();
            int index = protoypeInfos[0].PrototypeMeshes.Count;
            for (int i = 0; i < protoypeInfos.Length; i++)
            {
                PrototypeInfoData protInfo = protoypeInfos[i];
                
                for (int j = 0; j < protInfo.PrototypeMeshes.Count; j++)
                {
                    if (!Meshes.TryAdd(index, protInfo.PrototypeMeshes[j])) continue;
                    
                    meshesToIndex.Add(protInfo.PrototypeMeshes[j], index);
                    index++;
                }

                for (int j = 0; j < protInfo.Prototypes.Count; j++)
                {
                    PrototypeData prot = protInfo.Prototypes[j];
                    if (prot.MeshRot.MeshIndex == -1) continue;
                    if (prot.MeshRot.MeshIndex >= protInfo.PrototypeMeshes.Count) continue;
                    
                    int meshIndex = meshesToIndex[protInfo.PrototypeMeshes[prot.MeshRot.MeshIndex]];
                    PrototypeData prototypeData = new PrototypeData(
                        new MeshWithRotation(meshIndex, prot.MeshRot.Rot),
                        prot.PosX, prot.NegX, prot.PosY, prot.NegY, prot.PosZ, prot.NegZ,
                        prot.Weight, prot.MaterialIndexes)
                    {
                        Name_EditorOnly = Meshes[meshIndex].name
                    };

                    protInfo.Prototypes[j] = prototypeData;
                    if (protInfo.PrototypeTargetIndexesEditor.Contains(j))
                    {
                        protInfo.PrototypeTargetIndexes.Add(meshIndex);
                    }
                }
                
                for (int j = 0; j < protInfo.MarchingTable.Length; j++)
                {
                    for (int k = 0; k < protInfo.MarchingTable[j].Count; k++)
                    {
                        PrototypeData prot = protInfo.MarchingTable[j][k];
                        if (prot.MeshRot.MeshIndex == -1) continue;
                        if (prot.MeshRot.MeshIndex >= protInfo.PrototypeMeshes.Count) continue;

                        int meshIndex = meshesToIndex[protInfo.PrototypeMeshes[prot.MeshRot.MeshIndex]];
                        PrototypeData prototypeData = new PrototypeData(
                            new MeshWithRotation(meshIndex, prot.MeshRot.Rot),
                            prot.PosX, prot.NegX, prot.PosY, prot.NegY, prot.PosZ, prot.NegZ,
                            prot.Weight, prot.MaterialIndexes)
                        {
                            Name_EditorOnly = Meshes[meshIndex].name
                        };
                        protInfo.MarchingTable[j][k] = prototypeData;
                    }
                }
                
                protInfo.NotBottomPrototypes.Clear();
                
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