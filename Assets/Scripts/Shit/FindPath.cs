using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using static UnityEditor.Progress;

namespace Path
{
    public class FindPath : MonoBehaviour
    {
        [Header("Control")]
        [SerializeField]
        [Range(0.001f, 0.75f)]
        private float delay = 0.25f;

        [Header("Color")]
        [SerializeField]
        private Color normalColor;

        [SerializeField]
        private Gradient walkedGradient;

        [SerializeField]
        [Range(0.005f, 0.1f)]
        private float gradientStep = 0.05f;

        private Grid grid;

        // Key = the index of the cell, Value = the paths that have been atempted
        private Dictionary<Vector2Int, LinkedList<LinkedList<Vector2Int>>> triedDictionary = new Dictionary<Vector2Int, LinkedList<LinkedList<Vector2Int>>>();  
        
        private Vector2Int currentIndex;
        private LinkedList<Vector2Int> path = new LinkedList<Vector2Int>(); 

        private bool shouldStop = false;
        private float currentGradientStep = 0;
        private int tilesVisited = 0;

        private void OnValidate()
        {
            grid = FindObjectOfType<Grid>();
            currentIndex = new Vector2Int(0, 0);
        }

        public async void Run(bool clear = true)
        {
            if (clear)
            {
                Clear();
                shouldStop = false;

                AffectTile();
                await Task.Delay((int)(delay * 1000));
            }

            while (Move()) 
            {
                AffectTile();

                await Task.Delay((int)(delay * 1000));
               
                if (shouldStop)
                {
                    return;
                }
            } 

            if (!IsDone())
            {
                if (shouldStop)
                {
                    return;
                }

                await BackTrack();
            }
        }

        private async Task BackTrack()
        {
            var first = path.First.Value;
            path.RemoveFirst();

            if (!triedDictionary.ContainsKey(currentIndex))
            {
                var list = new LinkedList<LinkedList<Vector2Int>>();
                list.AddLast(new LinkedList<Vector2Int>(path));
                triedDictionary.Add(currentIndex, list);
            }
            else
            {
                triedDictionary[currentIndex].AddLast(new LinkedList<Vector2Int>(path));
            }

            grid.Cells[currentIndex.x, currentIndex.y].Visited = false;
            grid.ColorSquare(currentIndex, normalColor);

            tilesVisited--;
            currentGradientStep -= gradientStep;

            await Task.Delay((int)(delay * 1000));

            currentIndex = first;

            Run(false);
        }

        private bool IsDone()
        {
            if (tilesVisited >= grid.Cells.Length)
            {
                Debug.Log("<color=green>Complete!</color>");
                return true;
            }

            return false;
        }

        private void AffectTile()
        {
            tilesVisited++;
            grid.Cells[currentIndex.x, currentIndex.y].Visited = true;

            // Color
            currentGradientStep += gradientStep;
            if (currentGradientStep > 1.1f)
            {
                currentGradientStep = 0;
            }

            grid.ColorSquare(currentIndex, walkedGradient.Evaluate(currentGradientStep));
        }

        private bool Move()
        {
            List<Vector2Int> neighbours = GetPossibleNeighbours(currentIndex);

            for (int i = 0; i < neighbours.Count; i++)
            {
                if (!IsValid(neighbours[i]))
                {
                    neighbours.RemoveAt(i--);
                }
            }

            if (neighbours.Count <= 0)
            {
                return false;
            }

            path.AddFirst(currentIndex); // Adds where that cell came from

            int randIndex = UnityEngine.Random.Range(0, neighbours.Count);
            currentIndex = neighbours[randIndex];

            return true;
        }

        private bool IsValid(Vector2Int neighbour)
        {
            if (neighbour.x < 0 || neighbour.y < 0 || neighbour.x >= grid.Cells.GetLength(0) || neighbour.y >= grid.Cells.GetLength(1))
            {
                return false;
            }

            if (triedDictionary.TryGetValue(neighbour, out LinkedList<LinkedList<Vector2Int>> value))
            {
                string sP = "";
                foreach (var item in path)
                {
                    sP += item.ToString() + ", ";
                }

                foreach (var list in value)
                {
                    string sl = "";
                    foreach (var item in list)
                    {
                        sl += item.ToString() + ", ";
                    }

                    if (sP == sl)
                    {
                        return false;
                    }
                }
            }

            return !grid.Cells[neighbour.x, neighbour.y].Visited;
        }

        private List<Vector2Int> GetPossibleNeighbours(Vector2Int index)
        {
            return new List<Vector2Int>()
            {
                new Vector2Int(index.x + 1, index.y),
                new Vector2Int(index.x - 1, index.y),
                new Vector2Int(index.x, index.y + 1),
                new Vector2Int(index.x, index.y - 1)
            };
        }

        public void Clear()
        {
            tilesVisited = 0;
            currentGradientStep = 0;
            shouldStop = true;
            currentIndex = new Vector2Int(0, 0);
            path = new LinkedList<Vector2Int>();

            triedDictionary = new Dictionary<Vector2Int, LinkedList<LinkedList<Vector2Int>>>();

            grid.GenerateGrid();
        }
    }
}

