using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SkillTree.UI
{
    public class UISkillTreeHandler : MonoBehaviour
    {
        [Title("References")]
        [SerializeField]
        private SkillTreeBuilder skillTreeBuilder;
        
        [Title("Setup")]
        [SerializeField]
        private UISkillTreeNode nodePrefab;
        
        [SerializeField]
        private RectTransform nodeContainer;
        
        private List<UISkillTreeNode> nodes = new List<UISkillTreeNode>();

        private SkillTreeNode rootNode;

        private void OnEnable()
        {
            rootNode = skillTreeBuilder.BuildSkillTree();
            DisplayTree(rootNode);
            
            nodes[0].SetLocked(false);
        }

        public void DisplayTree(SkillTreeNode rootNode)
        {
            Stack<SkillTreeNode> stack = new Stack<SkillTreeNode>();
            stack.Push(rootNode);

            while (stack.TryPop(out SkillTreeNode node))
            {
                SpawnNode(node);

                for (int i = 0; i < node.Children.Count; i++)
                {
                    stack.Push(node.Children[i]);
                }
            }
        }

        private void SpawnNode(SkillTreeNode skillTreeNode)
        {
            UISkillTreeNode spawned = Instantiate(nodePrefab, nodeContainer);
            spawned.SetLocked(true);
            spawned.DisplaySkillNode(skillTreeNode.NodeDescription);
            spawned.OnClick += OnClick;

            nodes.Add(spawned);
            
            void OnClick()
            {
                UnlockNode(skillTreeNode);
                spawned.OnClick -= OnClick;
            }
        }

        public void UnlockTree(int[] unlockedIndexes)
        {
            int index = 0;
            Stack<SkillTreeNode> stack = new Stack<SkillTreeNode>();
            stack.Push(rootNode);

            while (stack.TryPop(out SkillTreeNode node))
            {
                if (node.Index == unlockedIndexes[index])
                {
                    UnlockNode(node);
                }

                index++;
                for (int i = 0; i < node.Children.Count; i++)
                {
                    stack.Push(node.Children[i]);
                }
            }
        }

        private void UnlockNode(SkillTreeNode node)
        {
            node.SkillNode.Unlock();
            nodes[node.Index].SetUnlocked();
            for (int i = 0; i < node.Children.Count; i++)
            {
                nodes[node.Children[i].Index].SetLocked(false);
            }
        }
    }

    public class SkillTreeNode
    {
        public SkillTreeNode Parent;
        public readonly List<SkillTreeNode> Children = new List<SkillTreeNode>();
        
        public ISkillNodeDescription NodeDescription;
        public ISkillNode SkillNode;
        
        public int Index;
    }
}