using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using SkillTree.UI;
using UnityEngine;

namespace SkillTree
{
    public class SkillTreeBuilder : MonoBehaviour
    {
        [Title("Setup")]
        [SerializeField]
        private SkillNodeUtility skillNodeUtility;

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Return the root node</returns>
        public SkillTreeNode BuildSkillTree()
        {
            Dictionary<SkillNodeData, SkillTreeNode> dataToNode = new Dictionary<SkillNodeData, SkillTreeNode>();

            for (int i = 0; i < skillNodeUtility.SkillNodes.Length; i++)
            {
                SkillNodeData skillNodeData = skillNodeUtility.SkillNodes[i];
                if (dataToNode.ContainsKey(skillNodeData)) continue;

                GetSkillTreeNode(skillNodeData, dataToNode);
            }

            SkillTreeNode rootNode = null;
            foreach (SkillTreeNode skillTreeNode in dataToNode.Values)
            {
                if (skillTreeNode.Parent == null)
                {
                    rootNode = skillTreeNode;
                }
            }

            if (rootNode == null)
            {
                Debug.LogError("Failed to build skill tree");
                return null;
            }

            SetIndexes(rootNode);
            
            return rootNode;
        }

        private void SetIndexes(SkillTreeNode rootNode)
        {
            int index = 0;
            Stack<SkillTreeNode> stack = new Stack<SkillTreeNode>();
            stack.Push(rootNode);

            while (stack.TryPop(out SkillTreeNode node))
            {
                node.Index = index;
                index++;
                for (int i = 0; i < node.Children.Count; i++)
                {
                    stack.Push(node.Children[i]);
                }
            }
        }

        private static SkillTreeNode GetSkillTreeNode(SkillNodeData skillNodeData, Dictionary<SkillNodeData, SkillTreeNode> dataToNode)
        {
            SkillTreeNode skillTreeNode = skillNodeData.GetSkillTreeNode();
            dataToNode.Add(skillNodeData, skillTreeNode);

            if (!skillNodeData.Requirement)
            {
                return skillTreeNode;
            }
            
            if (!dataToNode.TryGetValue(skillNodeData.Requirement, out SkillTreeNode parentNode))
            {
                parentNode = GetSkillTreeNode(skillNodeData.Requirement, dataToNode);
            }
            
            skillTreeNode.Parent = parentNode;
            parentNode.Children.Add(skillTreeNode);
            
            return skillTreeNode;
        }
    }
}