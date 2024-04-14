using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEditor.PlayerSettings;
using Debug = UnityEngine.Debug;

public class BuildingManager : MonoBehaviour
{
    [Title("Misc")]
    [SerializeField]
    private Fighter[] fighters;

    [Title("Prototypes")]
    [SerializeField]
    private PrototypeInfoCreator townPrototypeInfo;

    [Title("Mesh")]
    [SerializeField]
    private List<Material> mats;

    [Title("Debug")]
    [SerializeField]
    private bool DebugPropagate;

    [SerializeField, ShowIf(nameof(DebugPropagate)), Range(1, 1000)]
    private int Speed;

    private Cell[,,] cells;

    private Dictionary<Vector3Int, GameObject> spawnedMeshes = new Dictionary<Vector3Int, GameObject>();
    private List<GameObject> spawnedPossibilities = new List<GameObject>();
    private List<Building> spawnedBuildings = new List<Building>();

    private List<PrototypeData> prototypes = new List<PrototypeData>();
    private List<Vector3Int> cellsToCollapse = new List<Vector3Int>();
    private Stack<Vector3Int> cellStack = new Stack<Vector3Int>();

    private WaveFunction waveFunction;

    private PrototypeData emptyPrototype;
    private PrototypeData groundPrototype;

    private void OnEnable()
    {
        Events.OnBuildingBuilt += BuildingBuilt;
        waveFunction = FindObjectOfType<WaveFunction>();

        waveFunction.OnMapGenerated += Load;
    }

    private void OnDisable()
    {
        waveFunction.OnMapGenerated -= Load;

        Events.OnBuildingBuilt -= BuildingBuilt;
    }

    #region Loading

    private void Load()
    {
        Clear();

        if (!LoadPrototypeData())
        {
            Debug.LogError("No prototype data found");
            return;
        }

        LoadCells();
        return;
    }

    private void LoadCells()
    {
        cells = new Cell[waveFunction.GridSize.x, waveFunction.GridSize.y, waveFunction.GridSize.z];
        emptyPrototype = new PrototypeData(new MeshWithRotation(null, 0), "-1s", "-1s", "-1s", "-1s", "-1s", "-1s", 20, new int[0]);
        groundPrototype = new PrototypeData(new MeshWithRotation(null, 0), "-1s", "-1s", "v-1_0", "-1s", "-1s", "-1s", 20, new int[0]);

        for (int x = 0; x < waveFunction.GridSize.x; x++)
        {
            for (int y = 0; y < waveFunction.GridSize.y; y++)
            {
                for (int z = 0; z < waveFunction.GridSize.z; z++)
                {
                    Vector3 pos = new Vector3(x * waveFunction.GridScale.x, y * waveFunction.GridScale.y, z * waveFunction.GridScale.z);
                    cells[x, y, z] = new Cell(false, pos + transform.position, new List<PrototypeData>(prototypes));
                }
            }
        }
    }

    private bool LoadPrototypeData()
    {
        if (townPrototypeInfo == null)
        {
            Debug.LogError("Please enter prototype reference");
            return false;
        }

        prototypes = townPrototypeInfo.Prototypes;
        return prototypes.Count > 0;
    }

    #endregion


    public async void Query(Vector3 queryPosition)
    {
        cellsToCollapse = GetSurroundingCells(queryPosition);
        for (int i = 0; i < cellsToCollapse.Count; i++)
        {
            Vector3Int index = cellsToCollapse[i];
            cells[index.x, index.y, index.z] = new Cell(false, cells[index.x, index.y, index.z].Position, new List<PrototypeData>(prototypes));
            ValidDirections(index, out _).ForEach(x => cellStack.Push(x));

            if (spawnedMeshes.TryGetValue(index, out GameObject gm))
            {
                Destroy(gm);
                spawnedMeshes.Remove(index);
            }

            if (!cells[index.x, index.y - 1, index.z].Collapsed)
            {
                SetCell(index + Vector3Int.down, groundPrototype);
            }
        }
        await Task.Delay(100);
        await Propagate();
        
        while (!cellsToCollapse.All(x => cells[x.x, x.y, x.z].Collapsed))
        {
            await Iterate();
            await Task.Delay(10);
        }

        Debug.Log("Done");
    }

    #region Core
    public async Task Iterate()
    {
        Vector3Int index = GetLowestEntropyIndex();
        Debug.Log("Lowest Index: " + index);

        PrototypeData chosenPrototype = Collapse(cells[index.x, index.y, index.z]);
        SetCell(index, chosenPrototype);

        await Propagate();
        Debug.Log("Propogating");
    }

    private Vector3Int GetLowestEntropyIndex()
    {
        float lowestEntropy = 10000;
        Vector3Int index = new Vector3Int();

        for (int i = 0; i < cellsToCollapse.Count; i++)
        {
            Cell cell = cells[cellsToCollapse[i].x, cellsToCollapse[i].y, cellsToCollapse[i].z];
            if (cell.Collapsed)
            {
                continue;
            }

            float possibleMeshAmount = 0;
            float totalWeight = 0;
            for (int g = 0; g < cell.PossiblePrototypes.Count; g++)
            {
                totalWeight += cell.PossiblePrototypes[g].Weight;
            }

            float averageWeight = totalWeight / cell.PossiblePrototypes.Count;
            for (int g = 0; g < cell.PossiblePrototypes.Count; g++)
            {
                float distFromAverage = 1.0f - (cell.PossiblePrototypes[g].Weight / averageWeight);
                if (distFromAverage < 1.0f) distFromAverage *= distFromAverage; // Because of using the percentage as a distance, smaller weights weigh more, so this is is to try to correct that.

                possibleMeshAmount += Mathf.Lerp(0, 1, Mathf.Abs(distFromAverage));
            }

            if (possibleMeshAmount < lowestEntropy)
            {
                lowestEntropy = possibleMeshAmount;
                index = cellsToCollapse[i];
            }
        }

        return index;
    }

    private PrototypeData Collapse(Cell cell)
    {
        if (cell.PossiblePrototypes.Count == 0)
        {
            return emptyPrototype;
        }

        int totalCount = 0;
        for (int i = 0; i < cell.PossiblePrototypes.Count; i++)
        {
            totalCount += cell.PossiblePrototypes[i].Weight;
        }

        int index = 0;
        int randomIndex = UnityEngine.Random.Range(0, totalCount);
        for (int i = 0; i < cell.PossiblePrototypes.Count; i++)
        {
            randomIndex -= cell.PossiblePrototypes[i].Weight;
            if (randomIndex <= 0)
            {
                index = i;
                break;
            }
        }

        return cell.PossiblePrototypes[index];
    }

    private void SetCell(Vector3Int index, PrototypeData chosenPrototype)
    {
        cells[index.x, index.y, index.z] = new Cell(true, cells[index.x, index.y, index.z].Position, new List<PrototypeData>() { chosenPrototype });
        cellStack.Push(index);

        GameObject spawned = GenerateMesh(cells[index.x, index.y, index.z].Position, chosenPrototype);
        if (spawned != null)
        {
            spawnedMeshes.Add(index, spawned);
        }
    }

    public async Task Propagate()
    {
        while (cellStack.TryPop(out Vector3Int cellIndex))
        {
            Cell changedCell = cells[cellIndex.x, cellIndex.y, cellIndex.z];

            List<Vector3Int> validDirs = ValidDirections(cellIndex, out List<Direction> directions);

            for (int i = 0; i < validDirs.Count; i++)
            {
                Cell neighbour = cells[validDirs[i].x, validDirs[i].y, validDirs[i].z];
                Direction dir = directions[i];

                var constrainedPrototypes = Constrain(changedCell, neighbour, dir, out bool changed);

                if (changed)
                {
                    cells[validDirs[i].x, validDirs[i].y, validDirs[i].z] = new Cell(neighbour.Collapsed, neighbour.Position, constrainedPrototypes);
                    cellStack.Push(validDirs[i]);
                }
            }

            if (DebugPropagate)
            {
                DisplayPossiblePrototypes();
                await Task.Delay(Speed);
            }
        }
    }

    #endregion

    #region Constain

    private List<Vector3Int> ValidDirections(Vector3Int index, out List<Direction> directions)
    {
        List<Vector3Int> valids = new List<Vector3Int>();
        directions = new List<Direction>();

        // Right
        if (index.x + 1 < cells.GetLength(0))
        {
            valids.Add(index + Vector3Int.right);
            directions.Add(Direction.Right);
        }

        // Left
        if (index.x - 1 >= 0)
        {
            valids.Add(index + Vector3Int.left);
            directions.Add(Direction.Left);
        }

        // Up
        if (index.y + 1 < cells.GetLength(1))
        {
            valids.Add(index + Vector3Int.up);
            directions.Add(Direction.Up);
        }

        // Down
        if (index.y - 1 >= 0)
        {
            valids.Add(index + Vector3Int.down);
            directions.Add(Direction.Down);
        }

        // Forward
        if (index.z + 1 < cells.GetLength(2))
        {
            valids.Add(index + Vector3Int.forward);
            directions.Add(Direction.Forward);
        }

        // Backward
        if (index.z - 1 >= 0)
        {
            valids.Add(index + Vector3Int.back);
            directions.Add(Direction.Backward);
        }


        return valids;
    }

    private List<PrototypeData> Constrain(Cell changedCell, Cell affectedCell, Direction direction, out bool changed)
    {
        if (affectedCell.Collapsed)
        {
            changed = false;
            return null;
        }

        List<string> validKeys = new List<string>();
        for (int i = 0; i < changedCell.PossiblePrototypes.Count; i++)
        {
            switch (direction)
            {
                case Direction.Right:
                    validKeys.Add(changedCell.PossiblePrototypes[i].PosX);
                    break;

                case Direction.Left:
                    validKeys.Add(changedCell.PossiblePrototypes[i].NegX);
                    break;

                case Direction.Up:
                    validKeys.Add(changedCell.PossiblePrototypes[i].PosY);
                    break;

                case Direction.Down:
                    validKeys.Add(changedCell.PossiblePrototypes[i].NegY);
                    break;

                case Direction.Forward:
                    validKeys.Add(changedCell.PossiblePrototypes[i].PosZ);
                    break;

                case Direction.Backward:
                    validKeys.Add(changedCell.PossiblePrototypes[i].NegZ);
                    break;

            }
        }

        int removed = 0;
        for (int i = 0; i < affectedCell.PossiblePrototypes.Count; i++)
        {
            bool shouldRemove = false;
            switch (direction)
            {
                case Direction.Right:
                    shouldRemove = !CheckValidSocket(affectedCell.PossiblePrototypes[i].NegX, validKeys);

                    break;
                case Direction.Left:
                    shouldRemove = !CheckValidSocket(affectedCell.PossiblePrototypes[i].PosX, validKeys);

                    break;
                case Direction.Up:
                    shouldRemove = !CheckValidSocket(affectedCell.PossiblePrototypes[i].NegY, validKeys);

                    break;
                case Direction.Down:
                    shouldRemove = !CheckValidSocket(affectedCell.PossiblePrototypes[i].PosY, validKeys);

                    break;
                case Direction.Forward:
                    shouldRemove = !CheckValidSocket(affectedCell.PossiblePrototypes[i].NegZ, validKeys);

                    break;
                case Direction.Backward:
                    shouldRemove = !CheckValidSocket(affectedCell.PossiblePrototypes[i].PosZ, validKeys);

                    break;
            }

            if (shouldRemove)
            {
                affectedCell.PossiblePrototypes.RemoveAt(i--);
                removed++;
            }
        }

        changed = removed > 0;
        return affectedCell.PossiblePrototypes;
    }

    private bool CheckValidSocket(string key, List<string> validKeys)
    {
        if (key.Contains('v')) // Ex. v0_0
        {
            return validKeys.Contains(key);
        }
        else if (key.Contains('s')) // Ex. 0s
        {
            return validKeys.Contains(key);
        }
        else if (key.Contains('f')) // Ex. 0f
        {
            return validKeys.Contains(key.Replace("f", ""));
        }
        else // Ex. 0
        {
            string keyf = key + 'f';
            return validKeys.Contains(keyf);
        }
    }

    #endregion

    #region Utility

    private List<Vector3Int> GetSurroundingCells(Vector3 queryPosition)
    {
        List<Vector3Int> surrounding = new List<Vector3Int>();

        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3Int index = GetIndex(queryPosition + Vector3.right * x / 2.0f + Vector3.forward * z / 2.0f);
                surrounding.Add(index);
            }
        }

        return surrounding;
    }

    private Vector3Int GetIndex(Vector3 pos)
    {
        return new Vector3Int(Math.GetMultiple(pos.x, 2), Math.GetMultiple(pos.y, 2f), Math.GetMultiple(pos.z, 2));
    }

    public void DisplayPossiblePrototypes()
    {
        HidePossiblePrototypes();

        foreach (var cell in cells)
        {
            if (cell.Collapsed)
            {
                continue;
            }

            float scale = 1.0f / cell.PossiblePrototypes.Count;
            int removed = 0;
            for (int g = 0; g < cell.PossiblePrototypes.Count; g++)
            {
                if (cell.PossiblePrototypes[g].MeshRot.Mesh == null)
                {
                    removed++;
                    continue;
                }

                float offset = (1.0f / cell.PossiblePrototypes.Count) * (((float)(g + 1 - removed) * 2) - cell.PossiblePrototypes.Count);
                Vector3 pos = cell.Position + Vector3.right * offset;

                spawnedPossibilities.Add(GenerateMesh(pos, cell.PossiblePrototypes[g], scale));
            }
        }
    }


    public void HidePossiblePrototypes()
    {
        for (int i = 0; i < spawnedPossibilities.Count; i++)
        {
            DestroyImmediate(spawnedPossibilities[i]);
        }

        spawnedPossibilities.Clear();
    }


    private GameObject GenerateMesh(Vector3 position, PrototypeData prototypeData, float scale = 1)
    {
        if (prototypeData.MeshRot.Mesh == null)
        {
            return null;
        }

        GameObject gm = new GameObject();
        gm.AddComponent<MeshFilter>().mesh = prototypeData.MeshRot.Mesh;
        gm.AddComponent<MeshRenderer>().materials = mats.Where((x) => prototypeData.MaterialIndexes.Contains(mats.IndexOf(x))).ToArray();

        gm.transform.position = position;
        gm.transform.rotation = Quaternion.Euler(0, 90 * prototypeData.MeshRot.Rot, 0);
        gm.transform.SetParent(transform, true);

        gm.transform.localScale *= scale;

        return gm;
    }

    #endregion

    public void Clear()
    {
        
    }

    private void BuildingBuilt(Building arg0)
    {
        // Add to building cells
    }

    private void Building_OnDeath(Building building)
    {
        spawnedBuildings.Remove(building);
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
