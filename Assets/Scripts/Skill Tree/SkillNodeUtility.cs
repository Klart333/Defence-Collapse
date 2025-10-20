using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace SkillTree
{
    [CreateAssetMenu(fileName = "Skill Node Utility", menuName = "Skill Tree/Skill Node Utility", order = 0)]
    public class SkillNodeUtility : ScriptableObject
    {
        [Title("Skill Nodes")]
        [SerializeField]
        private SkillNodeData[] skillNodes;
        
        public SkillNodeData[] SkillNodes => skillNodes;
        
#if UNITY_EDITOR
        [Button]
        public void FindAllSkillNodeDatasInFolder()
        {
            // Get the path of the current scriptable object
            string currentPath = AssetDatabase.GetAssetPath(this);
            string directoryPath = Path.GetDirectoryName(currentPath);
    
            if (string.IsNullOrEmpty(directoryPath))
            {
                Debug.LogError("Could not determine the folder path of this utility object.");
                return;
            }
    
            // Find all SkillNodeData assets in this folder and subfolders
            string[] guids = AssetDatabase.FindAssets("t:SkillNodeData", new[] { directoryPath });
    
            List<SkillNodeData> skillNodeDatas = new List<SkillNodeData>();
    
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                SkillNodeData cardData = AssetDatabase.LoadAssetAtPath<SkillNodeData>(assetPath);
                if (cardData != null)
                {
                    skillNodeDatas.Add(cardData);
                }
            }
    
            skillNodes = skillNodeDatas.ToArray();
            // Mark the object as dirty so changes will be saved
            EditorUtility.SetDirty(this);
            Debug.Log($"Found {skillNodeDatas.Count} SkillNodeData assets in {directoryPath} and its subfolders.");
        }
#endif
    }
}