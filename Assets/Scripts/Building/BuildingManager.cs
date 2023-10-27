using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    [SerializeField]
    private Building[] houses;

    [SerializeField]
    private Fighter[] fighters;

    private List<Building> spawnedBuildings = new List<Building>();

    private BuildingCell[,,] buildingCells;

    private WaveFunction waveFunction;

    private void OnEnable()
    {
        Events.OnBuildingBuilt += BuildingBuilt;
        waveFunction = FindObjectOfType<WaveFunction>();

        LoadCells();
    }

    private void OnDisable()
    {
        Events.OnBuildingBuilt -= BuildingBuilt;
    }

    private void BuildingBuilt(Building building)
    {
        if (spawnedBuildings.Contains(building))
        {
            return;
        }

        Vector3Int index = GetIndex(building.transform.position);
        if (index.x < 0 || index.z < 0)
        {
            print("Outside bounds fix later");
            return;
        }
        building.transform.position = buildingCells[index.x, index.y, index.z].Position;
        UpdateCells(index, building, 1);
        SetBuildingState(building);

        List<BuildingCell> neighbours = GetNeighbours(index, out List<Direction> dirs);
        for (int i = 0; i < neighbours.Count; i++)
        {
            if (neighbours[i].Built == 1)
            {
                Vector3 pos = Vector3.Lerp(building.transform.position, neighbours[i].Position, 0.5f);
                Quaternion rot = GetRot(dirs[i]);
                Building biggerHouse = BuildBiggerHouse(1, buildingCells[index.x, index.y, index.z].BuiltBuilding, neighbours[i].BuiltBuilding, pos, rot);

                Vector3Int neighbourIndex = GetIndex(neighbours[i].Position);
                UpdateCells(neighbourIndex, biggerHouse, 2);
                UpdateCells(index, biggerHouse, 2);

                WaitThenCheck(index, neighbourIndex, dirs);

                break;
            }
        }

        building.OnDeath += Building_OnDeath;
    }

    private Building BuildBiggerHouse(int buildingIndex, Building smol1, Building smol2, Vector3 pos, Quaternion rot)
    {
        var biggerHouse = Instantiate(houses[buildingIndex], pos, rot);
        UpgradeBuildings(smol1, smol2, biggerHouse);

        biggerHouse.BuildingSize = buildingIndex;
        biggerHouse.BuildingLevel = 0;

        Events.OnBuildingBuilt(biggerHouse);
        return biggerHouse;
    }

    private void Building_OnDeath(Building building)
    {
        spawnedBuildings.Remove(building);
    }

    private static Vector3Int GetIndex(Vector3 pos)
    {
        return new Vector3Int(Math.GetMultiple(pos.x, 0.666f), Math.GetMultiple(pos.y, 2f), Math.GetMultiple(pos.z, 0.666f));
    }

    private void UpgradeBuildings(Building smol1, Building smol2, Building biggerHouse)
    {
        SetBuildingState(biggerHouse);

        for (int i = 0; i < smol1.SpawnedFighters.Count; i++)
        {
            Fighter fighter = smol1.SpawnedFighters[i];
            fighter.Building = biggerHouse;
            biggerHouse.SpawnedFighters.Add(fighter);
        }

        for (int i = 0; i < smol2.SpawnedFighters.Count; i++)
        {
            Fighter fighter = smol2.SpawnedFighters[i];
            fighter.Building = biggerHouse;
            biggerHouse.SpawnedFighters.Add(fighter);
        }
        Destroy(smol1.gameObject);
        Destroy(smol2.gameObject);

        spawnedBuildings.Remove(smol1);
        spawnedBuildings.Remove(smol2);
    }

    private void SetBuildingState(Building building)
    {
        spawnedBuildings.Add(building);

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
        Building biggerHouse = BuildBiggerHouse(2, buildingCells[index.x, index.y, index.z].BuiltBuilding, buildingCells[other.x, other.y, other.z].BuiltBuilding, pos, Quaternion.identity);

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
        buildingCells[pos.x, pos.y, pos.z] = new BuildingCell(buildingCells[pos.x, pos.y, pos.z].Position, built, newBuilding);
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
        Vector3 size = waveFunction.GridSize;
        buildingCells = new BuildingCell[Mathf.FloorToInt(size.x) * 3, Mathf.FloorToInt(size.y), Mathf.FloorToInt(size.z) * 3];

        for (int z = 0; z < size.z * 3; z++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x * 3; x++)
                {
                    Vector3 pos = new Vector3(x * 0.666f, y * 2, z * 0.666f);

                    buildingCells[x, y, z] = new BuildingCell(pos);
                }
            }
        }
    }

    #region Get Closest House

    public Vector3 GetClosestHouse(Vector3 pos, out Building building, out int buildingIndex, List<int> blackList = null)
    {
        if (blackList == null)
        {
            blackList = new List<int>();
        }

        float castleDist = Vector3.Distance(pos, waveFunction.SpawnedCastle.transform.position);

        if (spawnedBuildings.Count == 0)
        {
            buildingIndex = -1;
            building = null;
            return waveFunction.SpawnedCastle.transform.position;
        }

        float smallest = 2048;
        buildingIndex = 0;
        for (int i = 0; i < spawnedBuildings.Count; i++)
        {
            if (spawnedBuildings[i].transform.position.y > 2f || blackList.Contains(i))
            {
                continue;
            }

            var dist = Vector3.Distance(pos, spawnedBuildings[i].transform.position);
            if (dist < smallest)
            {
                smallest = dist;
                buildingIndex = i;
            }
        }

        if (smallest < castleDist)
        {
            building = spawnedBuildings[buildingIndex];
            return building.transform.position;
        }
        else
        {
            buildingIndex = -1;
            building = null;
            return waveFunction.SpawnedCastle.transform.position;
        }
    }

    #endregion
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