using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SkillTree.UI
{
    public class UISkillTreeLineHandler : MonoBehaviour
    {
        [Title("Setup")]
        [SerializeField]
        private UISkillTreeHandler handler;

        [SerializeField]
        private UIDrawLine skillLinePrefab;
        
        [SerializeField]
        private RectTransform skillLineContainer;

        private void OnEnable()
        {
            handler.OnTreeFinished += OnTreeFinished;
        }

        private void OnDisable()
        {
            handler.OnTreeFinished -= OnTreeFinished;
        }

        private void OnTreeFinished()
        {
            Stack<SkillTreeNode> stack = new Stack<SkillTreeNode>();
            stack.Push(handler.RootNode);

            while (stack.TryPop(out SkillTreeNode node))
            {
                for (int i = 0; i < node.Children.Count; i++)
                {
                    SpawnLine(handler.Nodes[node.Index], handler.Nodes[node.Children[i].Index]);
                    stack.Push(node.Children[i]);
                }
            }
        }

        private void SpawnLine(UISkillTreeNode from, UISkillTreeNode to)
        {
            UIDrawLine spawned = Instantiate(skillLinePrefab, skillLineContainer);
            spawned.SetLinePoints(from.transform as RectTransform, to.transform as RectTransform);
        }
    }
}