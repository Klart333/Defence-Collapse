using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    [SerializeField]
    private Building[] houses;

    [SerializeField]
    private Fighter[] fighters;

    private BuildingCell[,,] buildingCells;

    private void Start()
    {
        LoadCells();

        Events.OnBuildingBuilt += BuildingBuilt;
    }

    private void BuildingBuilt(Building building)
    {
        SetBuildingState(building);

        Vector3Int index = UpdateCells(new Vector3Int((int)building.transform.position.x, (int)building.transform.position.y, (int)building.transform.position.z), building, 1);
        if (index.x == -1)
        {
            print("Couldn't find");
            return;
        }

        List<BuildingCell> neighbours = GetNeighbours(index, out List<Direction> dirs);
        for (int i = 0; i < neighbours.Count; i++)
        {
            if (neighbours[i].Built == 1)
            {
                Vector3 pos = Vector3.Lerp(building.transform.position, neighbours[i].Position, 0.5f);
                Quaternion rot = GetRot(dirs[i]);
                var biggerHouse = Instantiate(houses[1], pos, rot);
                SetBuildingState(biggerHouse);

                Destroy(buildingCells[index.x, index.y, index.z].BuiltBuilding.gameObject);
                Destroy(neighbours[i].BuiltBuilding.gameObject);

                Vector3Int neighbourIndex = new Vector3Int((int)neighbours[i].Position.x, (int)neighbours[i].Position.y, (int)neighbours[i].Position.z);
                UpdateCells(neighbourIndex, biggerHouse, 2);
                UpdateCells(index, biggerHouse, 2);

                WaitThenCheck(index, neighbourIndex, dirs);

                break;
            }
        }

    }

    private void SetBuildingState(Building building)
    {
        if (building.transform.position.y >= 4)
        {
            building.SetState<ArcherState>();
        }
        else
        {
            building.Fighters = fighters;
            building.SetState<BarracksState>();
        }
    }

    private async void WaitThenCheck(Vector3Int index, Vector3Int neighbourIndex, List<Direction> dirs)
    {
        await Task.Delay(500);

        if (index.x != neighbourIndex.x) // Horizontal house
        {
            if (dirs.Contains(Direction.Forward) && buildingCells[index.x, index.y, index.z + 1].Built == 2 && buildingCells[neighbourIndex.x, neighbourIndex.y, neighbourIndex.z + 1].Built == 2)
            {
                BuildBig(index, neighbourIndex, new Vector3Int(index.x, index.y, index.z + 1), new Vector3Int(neighbourIndex.x, neighbourIndex.y, neighbourIndex.z + 1));
            }
            else if (dirs.Contains(Direction.Backward) && buildingCells[index.x, index.y, index.z - 1].Built == 2 && buildingCells[neighbourIndex.x, neighbourIndex.y, neighbourIndex.z - 1].Built == 2)
            {
                BuildBig(index, neighbourIndex, new Vector3Int(index.x, index.y, index.z - 1), new Vector3Int(neighbourIndex.x, neighbourIndex.y, neighbourIndex.z - 1));
            }
        }
        else // Vertical house
        {
            if (dirs.Contains(Direction.Right) && buildingCells[index.x + 1, index.y, index.z].Built == 2 && buildingCells[neighbourIndex.x + 1, neighbourIndex.y, neighbourIndex.z].Built == 2)
            {
                BuildBig(index, neighbourIndex, new Vector3Int(index.x + 1, index.y, index.z), new Vector3Int(neighbourIndex.x + 1, neighbourIndex.y, neighbourIndex.z));
            }
            else if (dirs.Contains(Direction.Left) && buildingCells[index.x - 1, index.y, index.z].Built == 2 && buildingCells[neighbourIndex.x - 1, neighbourIndex.y, neighbourIndex.z].Built == 2)
            {
                BuildBig(index, neighbourIndex, new Vector3Int(index.x - 1, index.y, index.z), new Vector3Int(neighbourIndex.x - 1, neighbourIndex.y, neighbourIndex.z));
            }
        }
    }

    private void BuildBig(Vector3Int index, Vector3Int neighbourIndex, Vector3Int other, Vector3Int other2)
    {
        Vector3 pos = Vector3.Lerp(buildingCells[index.x, index.y, index.z].BuiltBuilding.transform.position, buildingCells[other.x, other.y, other.z].BuiltBuilding.transform.position, 0.5f);
        var biggerHouse = Instantiate(houses[2], pos, Quaternion.identity);
        SetBuildingState(biggerHouse);

        Destroy(buildingCells[index.x, index.y, index.z].BuiltBuilding.gameObject);
        Destroy(buildingCells[other.x, other.y, other.z].BuiltBuilding.gameObject);

        UpdateCells(neighbourIndex, biggerHouse, 3);
        UpdateCells(index, biggerHouse, 3);
        UpdateCells(other, biggerHouse, 3);
        UpdateCells(other2, biggerHouse, 3);
    }

    private Quaternion GetRot(Direction direction)
    {
        switch (direction)
        {
            case Direction.Right:
                return Quaternion.Euler(0, 90, 0);
            case Direction.Left:
                return Quaternion.Euler(0, 90, 0);
            case Direction.Forward:
                return Quaternion.Euler(0, 0, 0);
            case Direction.Backward:
                return Quaternion.Euler(0, 0, 0);
            default:
                break;
        }

        return Quaternion.identity;
    }

    private Vector3Int UpdateCells(Vector3Int pos, Building newBuilding, int built)
    {
        buildingCells[pos.x, pos.y, pos.z] = new BuildingCell(pos, built, newBuilding);
        return pos;
    }

    private List<BuildingCell> GetNeighbours(Vector3Int index, out List<Direction> dirs)
    {
        List<BuildingCell> neighbours = new List<BuildingCell>();
        dirs = new List<Direction>();
        // Right
        if (index.x + 1 < buildingCells.GetLength(0))
        {
            neighbours.Add(buildingCells[index.x + 1, index.y, index.z]);
            dirs.Add(Direction.Right);
        }

        // Left
        if (index.x - 1 >= 0)
        {
            neighbours.Add(buildingCells[index.x - 1, index.y, index.z]);
            dirs.Add(Direction.Left);
        }

        // Forward
        if (index.z + 1 < buildingCells.GetLength(2))
        {
            neighbours.Add(buildingCells[index.x, index.y, index.z + 1]);
            dirs.Add(Direction.Forward);
        }

        // Backward
        if (index.z - 1 >= 0)
        {
            neighbours.Add(buildingCells[index.x, index.y, index.z - 1]);
            dirs.Add(Direction.Backward);
        }

        return neighbours;
    }

    private void LoadCells()
    {
        Vector3 size = FindObjectOfType<WaveFunction>().GridSize;
        buildingCells = new BuildingCell[Mathf.RoundToInt(size.x) * 2 + 1, Mathf.RoundToInt(size.y) * 2 + 1, Mathf.RoundToInt(size.z) * 2 + 1];

        for (int z = 0; z < size.z; z++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    Vector3 pos = new Vector3(x, y, z);

                    buildingCells[x, y, z] = new BuildingCell(pos);
                }
            }
        }
    }
}

public struct BuildingCell
{
    public Vector3 Position;
    public int Built;
    public Building BuiltBuilding;

    public BuildingCell(Vector3 position)
    {
        Position = position;
        Built = 0;
        BuiltBuilding = null;
    }

    public BuildingCell(Vector3 position, int built, Building builtBuilding) : this(position)
    {
        Built = built;
        BuiltBuilding = builtBuilding;
    }
}