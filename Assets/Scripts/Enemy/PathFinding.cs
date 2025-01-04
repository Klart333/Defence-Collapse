using System.Collections.Generic;
using DataStructures.Queue;
using System.Linq;
using UnityEngine;
using System;


public static class PathFinding
{
    public static Node[,] Map;
    public static Node[,] ThiccMap;

    private static Vector3Int[] Directions { get; } = 
    {
        new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), // (right, left)
        new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0), // (up, down)
        new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)  // (forward, backward)
    };

    private static Vector3Int[] XYDirections { get; } = 
    {
        new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), // (right, left)
        new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)  // (forward, backward)
    };


    #region 3D

    public static Vector3Int? BreadthFirstSearch(Vector3 startPos, HashSet<Vector3Int> targetPositions, Node[,,] map)
    {
        Vector3Int start = FindClosest(startPos, map);
        Dictionary<Vector3Int, Vector3Int> WalkedNodes = new Dictionary<Vector3Int, Vector3Int>();
        Queue<Vector3Int> NodeQueue = new Queue<Vector3Int>();
        NodeQueue.Enqueue(start);
        WalkedNodes.Add(start, start);

        while (NodeQueue.Count > 0)
        {
            Vector3Int current = NodeQueue.Dequeue();
            if (targetPositions.Contains(current))
            {
                return current;
            }

            List<Vector3Int> neighbours = GetNeighbours(current, map);

            for (int i = 0; i < neighbours.Count; i++)
            {
                if (!WalkedNodes.ContainsKey(neighbours[i]))
                {
                    WalkedNodes.Add(neighbours[i], current);
                    NodeQueue.Enqueue(neighbours[i]);
                }
            }
        }

        return null;
    }

    public static List<Node> FindPath(Vector3 startPos, Vector3 targetPos, Node[,,] map)
    {
        Vector3Int target = FindClosest(targetPos, map);
        Vector3Int start = FindClosest(startPos, map);

        if (!map[target.x, target.y, target.z].Walkable)
        {
            //Debug.Log("Could not find path");
            return null;
        }

        Dictionary<Vector3Int, Vector3Int> WalkedNodes = new Dictionary<Vector3Int, Vector3Int>();
        Dictionary<Vector3Int, int> Distance = new Dictionary<Vector3Int, int>();
        PriorityQueue<int, Vector3Int> NodeQueue = new PriorityQueue<int, Vector3Int>();

        WalkedNodes.Add(start, start);
        Distance.Add(start, 0);
        NodeQueue.Enqueue(1, start);

        while (NodeQueue.Count > 0)
        {
            Vector3Int current = NodeQueue.Dequeue();
            if (current == target)
            {
                break;
            }

            List<Vector3Int> neighbours = GetNeighbours(current, map);

            for (int i = 0; i < neighbours.Count; i++)
            {
                if (!WalkedNodes.ContainsKey(neighbours[i]))
                {
                    int heuristic = Mathf.RoundToInt(Mathf.Abs(map[neighbours[i].x, neighbours[i].y, neighbours[i].z].Position.x - targetPos.x) + Mathf.Abs(map[neighbours[i].x, neighbours[i].y, neighbours[i].z].Position.y - targetPos.y));
                    int dist = Distance[current] + 1;

                    Distance.Add(neighbours[i], dist);
                    WalkedNodes.Add(neighbours[i], current);
                    NodeQueue.Enqueue(dist + heuristic, neighbours[i]);
                }
            }
        }

        return GetPath(WalkedNodes, target, map);
    }


    private static List<Node> GetPath(Dictionary<Vector3Int, Vector3Int> walkedNodes, Vector3Int target, Node[,,] map)
    {
        List<Node> path = new List<Node>();
        Vector3Int current = target;

        if (!walkedNodes.ContainsKey(current))
        {
            Debug.Log("Could not find path");
            return null;
        }

        while (walkedNodes[current] != current)
        {
            path.Add(map[current.x, current.y, current.z]);

            current = walkedNodes[current];
        }

        for (int i = 0; i < path.Count - 1; i++)
        {
            Debug.DrawRay(path[i].Position, (path[i + 1].Position - path[i].Position).normalized * 1.666f, Color.cyan, 20, false);
        }
        return path;
    }

    private static List<Vector3Int> GetNeighbours(Vector3Int index, Node[,,] map)
    {
        List<Vector3Int> neighbours = new List<Vector3Int>();

        foreach (Vector3Int dir in Directions)
        {
            Vector3Int neighbourIndex = index + dir;
            if (map.IsInBounds(neighbourIndex) && map[neighbourIndex.x, neighbourIndex.y, neighbourIndex.z].Walkable)
            {
                neighbours.Add(neighbourIndex);
            }
        }

        return neighbours;
    }

    private static Vector3Int FindClosest(Vector3 target, Node[,,] map)
    {
        float closestDistance = float.PositiveInfinity;
        Vector3Int index = new Vector3Int();

        for (int x = 0; x < map.GetLength(0); x++)
        {
            for (int y = 0; y < map.GetLength(1); y++)
            {
                for (int z = 0; z < map.GetLength(2); z++)
                {
                    Vector3 pos = map[x, y, z].Position;
                    float dist = Vector3.SqrMagnitude(pos - target);
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        index.x = x;
                        index.y = y;
                        index.z = z;
                    }
                }
                
            }
        }

        return index;
    }

    #endregion

    #region 2D

    public static List<Node> FindPath(Vector3 startPos, Vector3 targetPos, Node[,] map)
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

    private static List<Node> GetPath(Dictionary<Vector2Int, Vector2Int> walkedNodes, Vector2Int target, Node[,] map)
    {
        List<Node> path = new List<Node>();
        Vector2Int current = target;

        if (!walkedNodes.ContainsKey(current))
        {
            Debug.Log("Could not find path");
            return null;
        }

        while (walkedNodes[current] != current)
        {
            path.Add(map[current.x, current.y]);

            current = walkedNodes[current];
        }

        for (int i = 0; i < path.Count - 1; i++)
        {
            Debug.DrawRay(path[i].Position, (path[i + 1].Position - path[i].Position).normalized * 0.666f, Color.cyan, 20);
        }
        return path;
    }

    private static List<Vector2Int> GetNeighbours(Vector2Int index, Node[,] map)
    {
        List<Vector2Int> neighbours = new List<Vector2Int>();

        foreach (Vector3Int dir in XYDirections)
        {
            Vector2Int neighbourIndex = index + new Vector2Int(dir.x, dir.z);
            if (map.IsInBounds(neighbourIndex) && map[neighbourIndex.x, neighbourIndex.y].Walkable)
            {
                neighbours.Add(neighbourIndex);
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

    #endregion
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
        private readonly LinkedList<PriorityQueueEntry<TPrio, TItem>> q = new();

        public int Count => q.Count();

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
