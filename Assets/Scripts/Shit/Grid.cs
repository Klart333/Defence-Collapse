using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Path
{
    public class Grid : MonoBehaviour
    {
        public Cell[,] Cells;

        [SerializeField]
        private int xSize;

        [SerializeField]
        private int ySize;

        [SerializeField]
        private Square cellPrefab;

        private Square[,] squares;

        [ContextMenu("Generate Grid")]
        public void GenerateGrid()
        {
            Clear();

            Cells = new Cell[xSize, ySize];
            squares = new Square[xSize, ySize];

            for (int y = 0; y < ySize; y++)
            {
                for (int x = 0; x < xSize; x++)
                {
                    Vector2 pos = new Vector2(x + 0.5f, y + 0.5f);
                    squares[x, y] = Instantiate(cellPrefab, pos, Quaternion.identity);
                    squares[x, y].name = $"{x}, {y}";
                    if (x > 0)
                    {
                        squares[x, y].transform.SetParent(squares[0, y].transform);
                    }
                    else
                    {
                        squares[x, y].transform.SetParent(transform);
                    }

                    Cells[x, y] = new Cell(false);
                }
            }
        }

        private void Clear()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                DestroyImmediate(transform.GetChild(i--).gameObject);
            }
        }

        public void ColorSquare(Vector2Int index, Color color)
        {
            squares[index.x, index.y].SetColor(color);
        }

        public async void ColorSquare(Vector2Int index, Color color1, Color color2, float delay)
        {
            squares[index.x, index.y].SetColor(color1);

            await Task.Delay((int)(delay * 1000));

            squares[index.x, index.y].SetColor(color2);

        }
    }

    public class Cell
    {
        public bool Visited;

        public Cell(bool visited)
        {
            Visited = visited;
        }
    }
}

