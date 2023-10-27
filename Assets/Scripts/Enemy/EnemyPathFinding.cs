using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DataStructures;
using DataStructures.Queue;

public static class EnemyPathFinding
{
    public static Node[,] Map;
    public static Node[,] ThiccMap;

    public static List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos, Node[,] map)
    {
        Vector2Int target = FindClosest(targetPos, map);
        Vector2Int start = FindClosest(startPos, map);

        if (!map[target.x, target.y].Walkable)
        {
            //Debug.Log("Could not find path");
            return null;
        }

        Dictionary<Vector2Int, Vector2Int> WalkedNodes = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Vector2Int, int> Distance = new Dictionary<Vector2Int, int>();
        PriorityQueue<int, Vector2Int> NodeQueue = new PriorityQueue<int, Vector2Int>();

        WalkedNodes.Add(start, start);
        Distance.Add(start, 0);
        NodeQueue.Enqueue(1, start);

        while (NodeQueue.Count > 0)
        {
            Vector2Int current = NodeQueue.Dequeue();
            if (current == target)
            {
                break;
            }

            List<Vector2Int> neighbours = GetNeighbours(current, map);

            for (int i = 0; i < neighbours.Count; i++)
            {
                if (!WalkedNodes.ContainsKey(neighbours[i]))    
                {
                    int heuristic = Mathf.RoundToInt(Mathf.Abs(map[neighbours[i].x, neighbours[i].y].Position.x - targetPos.x) + Mathf.Abs(map[neighbours[i].x, neighbours[i].y].Position.y - targetPos.y));
                    int dist = Distance[current] + 1;

                    Distance.Add(neighbours[i], dist);
                    WalkedNodes.Add(neighbours[i], current);
                    NodeQueue.Enqueue(dist + heuristic, neighbours[i]);
                }
            }
        }

        return GetPath(WalkedNodes, target, map);
    }

    private static List<Vector3> GetPath(Dictionary<Vector2Int, Vector2Int> walkedNodes, Vector2Int target, Node[,] map)
    {
        List<Vector3> path = new List<Vector3>();
        Vector2Int current = target;

        if (!walkedNodes.ContainsKey(current))
        {
            Debug.Log("Could not find path");
            return null;
        }

        while (walkedNodes[current] != current)
        {
            path.Add(map[current.x, current.y].Position);

            current = walkedNodes[current];
        }

        for (int i = 0; i < path.Count - 1; i++)
        {
            Debug.DrawRay(path[i], (path[i + 1] - path[i]).normalized * 0.666f, Color.cyan, 20);
        }
        return path;
    }

    private static List<Vector2Int> GetNeighbours(Vector2Int index, Node[,] map)
    {
        List<Vector2Int> neighbours = new List<Vector2Int>();

        // Left
        if (index.x > 0)
        {
            if (map[index.x - 1, index.y].Walkable)
            {
                neighbours.Add(new Vector2Int(index.x - 1, index.y));
            }
        }

        // Right
        if (index.x + 1 < map.GetLength(0))
        {
            if (map[index.x + 1, index.y].Walkable)
            {
                neighbours.Add(new Vector2Int(index.x + 1, index.y));
            }
        }

        // Backward
        if (index.y > 0)
        {
            if (map[index.x, index.y - 1].Walkable)
            {
                neighbours.Add(new Vector2Int(index.x, index.y - 1));
            }
        }

        // Right
        if (index.y + 1 < map.GetLength(1))
        {
            if (map[index.x, index.y + 1].Walkable)
            {
                neighbours.Add(new Vector2Int(index.x, index.y + 1));
            }
        }

        return neighbours;
    }

    private static Vector2Int FindClosest(Vector3 target, Node[,] map)
    {
        float closestDistance = float.PositiveInfinity;
        Vector2Int index = new Vector2Int();

        for (int i = 0; i < map.GetLength(0); i++)
        {
            for (int g = 0; g < map.GetLength(1); g++)
            {
                float dist = Mathf.Abs(target.x - map[i, g].Position.x) + Mathf.Abs(target.z - map[i, g].Position.z);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    index.x = i;
                    index.y = g;
                }
            }
        }

        return index;
    }

}


namespace DataStructures.Queue
{
    public class PriorityQueueEntry<TPrio, TItem>
    {
        public TPrio p { get; }
        public TItem data { get; }
        public PriorityQueueEntry(TPrio p, TItem data)
        {
            this.p = p;
            this.data = data;
        }
    }

    public class PriorityQueue<TPrio, TItem> where TPrio : IComparable
    {
        private LinkedList<PriorityQueueEntry<TPrio, TItem>> q;

        public PriorityQueue()
        {
            q = new LinkedList<PriorityQueueEntry<TPrio, TItem>>();
        }

        public int Count { get { return q.Count(); } }

        public void Enqueue(TPrio p, TItem data)
        {
            if (q.Count == 0)
            {
                q.AddFirst(new PriorityQueueEntry<TPrio, TItem>(p, data));
                return;
            }
            // This is a bit classical C but whatever
            LinkedListNode<PriorityQueueEntry<TPrio, TItem>> current = q.First;
            while (current != null)
            {
                if (current.Value.p.CompareTo(p) >= 0)
                {
                    q.AddBefore(current, new PriorityQueueEntry<TPrio, TItem>(p, data));
                    return;
                }
                current = current.Next;
            }
            q.AddLast(new PriorityQueueEntry<TPrio, TItem>(p, data));
        }

        public TItem Dequeue()
        {
            // LinkedList -> LinkedListNode -> PriorityQueueEntry -> data
            var ret = q.First.Value.data;
            q.RemoveFirst();
            return ret;
        }
    }
}
