using System.Collections.Generic;
using Sirenix.OdinInspector;
using SkillTree.UI;
using UnityEngine;
using Variables;

namespace SkillTree
{
    [CreateAssetMenu(fileName = "New_SkillNode", menuName = "Skill Tree/Skill Node Data", order = 0)]
    public class SkillNodeData : SerializedScriptableObject, ISkillNodeDescription
    {
        [Title("Description")]
        [SerializeField]
        private StringReference title;
        
        [SerializeField]
        private StringReference description;
        
        [SerializeField]
        private SpriteReference icon;
        
        [Title("Skill Node")]
        [SerializeField]
        private ISkillNode skillNode;
        
        [Title("Requirements")]
        [SerializeField]
        private SkillNodeData requirement;
        
        public SkillNodeData Requirement => requirement;
        public string Description => description.Value;
        public string Title => title.Value;
        public Sprite Icon => icon.Value;
        public float ExpCost { get; }

        public SkillTreeNode GetSkillTreeNode()
        {
            return new SkillTreeNode
            {
                NodeDescription = this,
                SkillNode = skillNode.Copy(skillNode),
            };
        }
    }
}