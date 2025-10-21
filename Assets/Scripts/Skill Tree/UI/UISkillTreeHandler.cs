using Random = Unity.Mathematics.Random;

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Gameplay.Event;
using UnityEngine;
using System;

namespace SkillTree.UI
{
    public class UISkillTreeHandler : MonoBehaviour
    {
        public event Action OnTreeFinished;
        
        [Title("References")]
        [SerializeField]
        private SkillTreeBuilder skillTreeBuilder;
        
        [Title("Setup")]
        [SerializeField]
        private UISkillTreeNode nodePrefab;
        
        [SerializeField]
        private RectTransform nodeContainer;

        [Title("Positioning")]
        [SerializeField]
        private Vector2 nodeSpacing;
        
        [Title("Random")]
        [SerializeField, Min(1)]
        private int seed;

        [Title("Debug")]
        [SerializeField]
        private float delayFactor = 1.0f;
        
        [SerializeField]
        private float spawnDelay = 0.5f;
        
        private List<SkillNodePosition> positionNodes = new List<SkillNodePosition>();
        private Queue<SkillTreeNode> unbalancedNodes = new Queue<SkillTreeNode>();

        private Random random;
        private bool hasCreatedTree;
        
        private Vector2 nodeSize;
        
        public SkillTreeNode RootNode { get; private set; }
        public List<UISkillTreeNode> Nodes { get; } = new List<UISkillTreeNode>();

        private void OnEnable()
        {
            if (hasCreatedTree)
            {
                return;
            }
            
            random = Random.CreateFromIndex((uint)seed);
            nodeSize = nodePrefab.GetComponent<RectTransform>().rect.size;
            
            RootNode = skillTreeBuilder.BuildSkillTree();
            DisplayTree(RootNode).Forget();
            
            Nodes[0].SetLocked(false);
            
            hasCreatedTree = true;
        }

        public async UniTaskVoid DisplayTree(SkillTreeNode rootNode)
        {
            Stack<SkillTreeNode> stack = new Stack<SkillTreeNode>();
            stack.Push(rootNode);

            while (stack.TryPop(out SkillTreeNode node))
            {
                await SpawnNode(node);

                for (int i = 0; i < node.Children.Count; i++)
                {
                    stack.Push(node.Children[i]);
                }

                await UniTask.Delay(TimeSpan.FromSeconds(spawnDelay * delayFactor));
            }
            
            OnTreeFinished?.Invoke();
        }

        private async UniTask SpawnNode(SkillTreeNode skillTreeNode)
        {
            Debug.Log($"Spawning Index: {skillTreeNode.Index}, Parent: {skillTreeNode.Parent?.Index}");
            
            UISkillTreeNode spawned = Instantiate(nodePrefab, nodeContainer);
            Nodes.Add(spawned);

            Vector2 position = GetPosition(skillTreeNode);
            SkillNodePosition positionNode = new SkillNodePosition(position, nodeSize);
            positionNodes.Add(positionNode);
            
            spawned.SetLocked(true);
            spawned.DisplaySkillNode(skillTreeNode.NodeDescription, positionNode, skillTreeNode.Index);
            spawned.OnClick += OnClick;
            
            unbalancedNodes.Enqueue(skillTreeNode);
            await BalanceNodePositions();

            
            void OnClick()
            {
                UnlockNode(skillTreeNode);
                spawned.OnClick -= OnClick;
            }
        }

        private async UniTask BalanceNodePositions()
        {
            while (unbalancedNodes.TryDequeue(out SkillTreeNode node))
            {
                if (node.Parent == null) continue;
                
                Debug.Log("Balance node positions, from index: " + node.Parent.Index);
            
                SkillTreeNode parent = node.Parent;
                int boundingBoxDepth = 0;
                while (!await BoundingBoxBalance(parent, boundingBoxDepth, parent.Parent == null))
                {
                    Debug.Log("GOING TO PARENT!");
                    parent = parent.Parent;
                    boundingBoxDepth++;

                    if (parent == null)
                    {
                        Debug.LogError("Failed to balance");
                        break;
                    }
                }
            }
        }

        private async UniTask<bool> BoundingBoxBalance(SkillTreeNode parentNode, int boundingBoxDepth, bool isFinal)
        {
            Debug.Log("Balancing");
            await UniTask.Delay(TimeSpan.FromSeconds(0.1f * delayFactor));

            float totalHeight = 0;
            List<Rect> boundingBoxes = new List<Rect>();
            List<SkillTreeNode> movedNodes = new List<SkillTreeNode>();
            for (int i = 0; i < parentNode.Children.Count; i++)
            {
                if (Nodes.Count <= parentNode.Children[i].Index) continue;

                Rect rect = GetBoundingBox(parentNode.Children[i], boundingBoxDepth);
                totalHeight += rect.height;
                
                boundingBoxes.Add(rect);
                movedNodes.Add(parentNode.Children[i]);
            }
            
            totalHeight += nodeSpacing.y * (movedNodes.Count - 1);
            float parentY = positionNodes[parentNode.Index].Position.y;
            float startY = parentY + totalHeight / 2f;
            float currentY = startY;
            
            // Move All
            for (int i = 0; i < movedNodes.Count; i++)
            {
                float halfHeight = boundingBoxes[i].height / 2f;
                currentY -= halfHeight;
        
                float heightOffset = positionNodes[movedNodes[i].Index].Position.y - currentY;
                MoveNode(movedNodes[i], Vector2.down * heightOffset);
                
                currentY -= halfHeight + nodeSpacing.y;
            }            
            
            await UniTask.Delay(TimeSpan.FromSeconds(0.2f * delayFactor));
            
            // Check overlap
            if (IsNodeOrChildOverlapping(parentNode, out SkillTreeNode overlappingChild))
            {
                if (isFinal)
                {
                    unbalancedNodes.Enqueue(overlappingChild);
                }
                return false;
            }
            
            for (int i = 0; i < movedNodes.Count; i++)
            {
                positionNodes[movedNodes[i].Index].OnPositionChanged?.Invoke();
            }
                
            return true;
        }

        private Rect GetBoundingBox(SkillTreeNode node, int boundingBoxDepth)
        {
            SkillNodePosition positionNode = positionNodes[node.Index];
            Rect rect = new Rect(positionNode.Position, positionNode.Size);
            if (boundingBoxDepth == 0)
            {
                return rect;
            }

            for (int i = 0; i < node.Children.Count; i++)
            {
                if (Nodes.Count <= node.Children[i].Index) continue;

                Rect childRect = GetBoundingBox(node.Children[i], boundingBoxDepth - 1);
                if (childRect.xMin < rect.xMin) rect.xMin = childRect.xMin; 
                if (childRect.yMin < rect.yMin) rect.yMin = childRect.yMin;
                if (childRect.xMax > rect.xMax) rect.xMax = childRect.xMax;
                if (childRect.yMax > rect.yMax) rect.yMax = childRect.yMax;
            }
            
            return rect;
        }

        private bool IsNodeOrChildOverlapping(SkillTreeNode nodeToCheck, out SkillTreeNode overlappingChild)
        {
            Stack<SkillTreeNode> stack = new Stack<SkillTreeNode>();
            stack.Push(nodeToCheck);
            
            while (stack.TryPop(out SkillTreeNode node))
            {
                if (Nodes.Count <= node.Index) continue;
                
                if (CheckOverlap(node))
                {
                    overlappingChild = node;
                    return true;
                }

                for (int i = 0; i < node.Children.Count; i++)
                {
                    stack.Push(node.Children[i]);
                }
            }

            overlappingChild = null;
            return false;
        }

        /// <summary>
        /// Is Silent
        /// </summary>
        private void MoveNode(SkillTreeNode nodeToMove, Vector2 move)
        {
            Stack<SkillTreeNode> stack = new Stack<SkillTreeNode>();
            stack.Push(nodeToMove);

            while (stack.TryPop(out SkillTreeNode node))
            {
                if (Nodes.Count <= node.Index) continue;
                
                positionNodes[node.Index].Position += move;

#if UNITY_EDITOR
                positionNodes[node.Index].OnPositionChanged?.Invoke(); 
#endif

                for (int i = 0; i < node.Children.Count; i++)
                {
                    stack.Push(node.Children[i]);
                }
            }
        }

        private Vector2 GetPosition(SkillTreeNode node)
        {
            if (node.Parent == null)
            {
                return Vector2.zero;
            }

            // Place to the right
            SkillNodePosition nodeParent = positionNodes[node.Parent.Index];
            Vector2 parentPosition = nodeParent.Position;
            Vector2 childPosition = parentPosition + Vector2.right * (nodeSize.x + nodeSpacing.x);

            while (CheckOverlap(node, childPosition, out Vector2 otherPos))
            {
                childPosition.y = otherPos.y + nodeSize.y + nodeSpacing.y;
            }
            
            return childPosition;    
            
        }

        private bool CheckOverlap(SkillTreeNode node, Vector2 childPosition, out Vector2 otherPos)
        {
            for (int i = 0; i < Nodes.Count; i++)
            {
                if (i == node.Index) continue;
                
                SkillNodePosition otherRect = positionNodes[i];
                otherPos = otherRect.Position;

                // AABB check
                if (childPosition.x < otherPos.x + nodeSize.x &&
                    childPosition.x + nodeSize.x > otherPos.x &&
                    childPosition.y < otherPos.y + nodeSize.y &&
                    childPosition.y + nodeSize.y > otherPos.y)
                {
                    return true;
                }
            }

            otherPos = default;
            return false;
        }
        
        private bool CheckOverlap(SkillTreeNode node)
        {
            Vector2 nodePosition = positionNodes[node.Index].Position;
            
            for (int i = 0; i < Nodes.Count; i++)
            {
                if (i == node.Index) continue;
                
                SkillNodePosition otherRect = positionNodes[i];
                Vector2 otherPos = otherRect.Position;

                // AABB check
                if (nodePosition.x < otherPos.x + nodeSize.x &&
                    nodePosition.x + nodeSize.x > otherPos.x &&
                    nodePosition.y < otherPos.y + nodeSize.y &&
                    nodePosition.y + nodeSize.y > otherPos.y)
                {
                    return true;
                }
            }

            return false;
        }

        public void UnlockTree(int[] unlockedIndexes)
        {
            int index = 0;
            Stack<SkillTreeNode> stack = new Stack<SkillTreeNode>();
            stack.Push(RootNode);

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
            Nodes[node.Index].SetUnlocked();
            for (int i = 0; i < node.Children.Count; i++)
            {
                Nodes[node.Children[i].Index].SetLocked(false);
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

    public class SkillNodePosition
    {
        public EventAction OnPositionChanged;
        
        public Vector2 Position { get; set; }
        public Vector2 Size { get; set; }

        public SkillNodePosition(Vector2 position, Vector2 size)
        {
            Position = position;
            Size = size;
        }
    }
}