using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace WaveFunctionCollapse
{
    [InlineEditor]
    [CreateAssetMenu(fileName = "New PrototypeInfoData", menuName = "PrototypeInfoData", order = 0)]
    public class PrototypeInfoData : SerializedScriptableObject
    {
        [OdinSerialize] 
        public List<PrototypeData> Prototypes { get; set; } = new();

        [OdinSerialize]
        public List<DicData> SocketList { get; set; } = new();

        [OdinSerialize]
        public List<DicData> VerticalSocketList { get; set; } = new();

        [OdinSerialize]
        public List<int> NotAllowedForBottom { get; set; } = new();

        [OdinSerialize]
        public List<int> OnlyAllowedForBottom  { get; set; } = new();
    
        [OdinSerialize]
        public List<int> NotAllowedForSides  { get; set; } = new();

        [OdinSerialize]
        public List<PrototypeData>[] MarchingTable { get; set; }

#if UNITY_EDITOR
        public void Clear()
        {
            Prototypes.Clear();
            SocketList.Clear();
            VerticalSocketList.Clear();
            NotAllowedForBottom.Clear();
            OnlyAllowedForBottom.Clear();
            NotAllowedForSides.Clear();
            MarchingTable = Array.Empty<List<PrototypeData>>();
            
            notBottomPrototypes = null;
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        [Button]
        public void Save()
        {
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
        }
#endif

        
        [SerializeField, ReadOnly]
        private List<PrototypeData> notBottomPrototypes;
        public List<PrototypeData> NotBottomPrototypes
        {
            get
            {
                if (notBottomPrototypes is null || notBottomPrototypes.Count == 0)
                {
                    notBottomPrototypes = new List<PrototypeData>(Prototypes);
                    for (int i = notBottomPrototypes.Count - 1; i >= 0; i--)
                    {
                        if (OnlyAllowedForBottom.Contains(i))
                        {
                            notBottomPrototypes.RemoveAt(i);
                        }
                    }
                }

                return notBottomPrototypes;
            }
        }
    }
}