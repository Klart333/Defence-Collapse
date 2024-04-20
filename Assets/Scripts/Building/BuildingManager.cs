using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class BuildingManager : MonoBehaviour 
{
    [Title("Misc")]
    [SerializeField]
    private Fighter[] fighters;

    [Title("Prototypes")]
    [SerializeField]
    private PrototypeInfoCreator townPrototypeInfo;

    [SerializeField]
    private PrototypeInfoCreator pathPrototypeInfo;

    [Title("Mesh")]
    [SerializeField]
    private List<Material> mats;

    [Title("Castle")]
    [SerializeField]
    private int CastleIndex = 20;

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
    private BuildingAnimator buildingAnimator;

    private PrototypeData emptyPrototype;
    private PrototypeData groundPrototype;
    private PrototypeData groundPathPrototype;
    //private PrototypeData[] portalPathPrototypes;

    private void OnEnable()
    {
        Events.OnBuildingBuilt += BuildingBuilt;
        waveFunction = FindObjectOfType<WaveFunction>();
        buildingAnimator = GetComponent<BuildingAnimator>();

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
        groundPathPrototype = new PrototypeData(new MeshWithRotation(null, 0), "-1s", "-1s", "v-2_0", "-1s", "-1s", "-1s", 20, new int[0]);
        // Still here if I can't spawn the road in later because of art reasons
        /*portalPathPrototypes = new PrototypeData[4] 
        {
            new PrototypeData(new MeshWithRotation(null, 0), "-1s", "-1s", "v-2_0", "-1s", "-1s", "0s", 20, new int[0]),
            new PrototypeData(new MeshWithRotation(null, 1), "-1s", "0s", "v-2_0", "-1s", "-1s", "-1s", 20, new int[0]),
            new PrototypeData(new MeshWithRotation(null, 2), "-1s", "-1s", "v-2_0", "-1s", "0s", "-1s", 20, new int[0]),
            new PrototypeData(new MeshWithRotation(null, 3), "0s", "-1s", "v-2_0", "-1s", "-1s", "-1s", 20, new int[0])
        };*/

        for (int z = 0; z < waveFunction.GridSize.z; z++)
        {
            for (int y = 0; y < waveFunction.GridSize.y; y++)
            {
                for (int x = 0; x < waveFunction.GridSize.x; x++)
                {
                    Vector3 pos = new Vector3(x * waveFunction.GridScale.x, y * waveFunction.GridScale.y, z * waveFunction.GridScale.z);
                    cells[x, y, z] = new Cell(false, pos + transform.position, new List<PrototypeData>(prototypes));
                }
            }
        }

        for (int z = 0; z < waveFunction.GridSize.z; z++)
        {
            for (int y = 0; y < waveFunction.GridSize.y; y++)
            {
                for (int x = 0; x < waveFunction.GridSize.x; x++)
                {
                    Cell groundCell = waveFunction.GetCellAtIndex(z, y, x);
                    if (groundCell.PossiblePrototypes[0].MeshRot.Mesh == null) // It's air
                    {

                    }
                    else if (groundCell.PossiblePrototypes[0].MeshRot.Mesh.name == "Ground_Portal") // yay hard coded names :))))
                    {
                        int index = 16 + ((groundCell.PossiblePrototypes[0].MeshRot.Rot + 1) % 4);
                        SetCell(new Vector3Int(x, y, z), pathPrototypeInfo.Prototypes[index]);
                        SetCell(new Vector3Int(x, y - 1, z), groundPathPrototype);
                        switch (index) // Gotta make it possible for the WFC to see a future
                        {
                            case 16:
                                SetCell(new Vector3Int(x + 1, y - 1, z), groundPathPrototype);
                                break;
                            case 17:
                                SetCell(new Vector3Int(x, y - 1, z - 1), groundPathPrototype);
                                break;
                            case 18:
                                SetCell(new Vector3Int(x - 1, y - 1, z), groundPathPrototype);
                                break;
                            case 19:
                                SetCell(new Vector3Int(x, y - 1, z + 1), groundPathPrototype);
                                break;
                            default:
                                break;
                        }
                    }
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
        prototypes.AddRange(pathPrototypeInfo.Prototypes);
        prototypes.RemoveAt(prototypes.Count - 1);
        return prototypes.Count > 0;
    }

    #endregion

    public async void Query(Vector3 queryPosition)
    {
        cellsToCollapse = GetSurroundingCells(queryPosition);
        MakeBuildable(cellsToCollapse, groundPrototype);
        await Propagate();

        while (!cellsToCollapse.All(x => cells[x.x, x.y, x.z].Collapsed))
        {
            await Iterate();
            //await Task.Delay(10);
        }
    }

    private void MakeBuildable(List<Vector3Int> cellsToCollapse, PrototypeData buildableProt, string key = "v-1_0") // Should only override city tiles and built roads, nothing else
    {
        for (int i = 0; i < cellsToCollapse.Count; i++)
        {
            Vector3Int index = cellsToCollapse[i];
            cells[index.x, index.y, index.z] = new Cell(false, cells[index.x, index.y, index.z].Position, new List<PrototypeData>(prototypes));
            ValidDirections(index, out _, Direction.Up).ForEach(x => cellStack.Push(x));

            if (spawnedMeshes.TryGetValue(index, out GameObject gm))
            {
                Destroy(gm);
                spawnedMeshes.Remove(index);
            }

            if (!cells[index.x, index.y - 1, index.z].Collapsed)
            {
                SetCell(index + Vector3Int.down, buildableProt);
            }
            else
            {
                ChangeTopKey(index + Vector3Int.down, key);
            }
        }
    }

    public async void PlaceCastle(Vector3 minQueryPosition, Vector3 maxQueryPosition)
    {
        cellsToCollapse = GetAllCells(minQueryPosition, maxQueryPosition);
        MakeBuildable(cellsToCollapse, groundPrototype);

        SetCell(cellsToCollapse[4], prototypes[CastleIndex]);
        await Propagate();

        while (!cellsToCollapse.All(x => cells[x.x, x.y, x.z].Collapsed))
        {
            await Iterate();
        }
    }

    public async void BuildPath(Vector3 mousePos)
    {
        cellsToCollapse = new List<Vector3Int>() { GetIndex(mousePos) };
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                if ((z != 0 && x != 0) || z == x || !cells.IsInBounds(cellsToCollapse[0].x + x, 0, cellsToCollapse[0].z + z)) continue;

                Vector3Int index = cellsToCollapse[0] + new Vector3Int(x, 0, z);
                if (cells[index.x, index.y, index.z].Collapsed && cells[index.x, index.y, index.z].PossiblePrototypes[0].MeshRot.Mesh && cells[index.x, index.y, index.z].PossiblePrototypes[0].MeshRot.Mesh.name.Contains("Path"))
                {
                    cellsToCollapse.Add(index);
                }
            }
        }

        MakeBuildable(cellsToCollapse, groundPathPrototype, "v-2_0");
        await Propagate();

        while (!cellsToCollapse.All(x => cells[x.x, x.y, x.z].Collapsed))
        {
            await Iterate();
        }
    }

    #region Core
    public async Task Iterate()
    {
        Vector3Int index = GetLowestEntropyIndex();

        PrototypeData chosenPrototype = Collapse(cells[index.x, index.y, index.z]);
        SetCell(index, chosenPrototype);

        await Propagate();
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

                possibleMeshAmount += Mathf.Lerp(1, 0, Mathf.Abs(distFromAverage));
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

        int randomIndex = UnityEngine.Random.Range(0, totalCount);
        int index = randomIndex;
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

    private void ChangeTopKey(Vector3Int index, string key)
    {
        PrototypeData prot = cells[index.x, index.y, index.z].PossiblePrototypes[0];
        if (prot.PosY == key) return;

        PrototypeData changedProt = new PrototypeData(new MeshWithRotation(prot.MeshRot.Mesh, prot.MeshRot.Rot), prot.PosX, prot.NegX, key, prot.NegY, prot.PosZ, prot.NegZ, prot.Weight, new int[0]);
        cells[index.x, index.y, index.z] = new Cell(true, cells[index.x, index.y, index.z].Position, new List<PrototypeData>() { changedProt });
        cellStack.Push(index);
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

    private List<Vector3Int> ValidDirections(Vector3Int index, out List<Direction> directions, Direction exlcude = Direction.None) // Exclude should be a flag
    {
        List<Vector3Int> valids = new List<Vector3Int>();
        directions = new List<Direction>();

        // Right
        if (index.x + 1 < cells.GetLength(0) && Direction.Right != exlcude)
        {
            valids.Add(index + Vector3Int.right);
            directions.Add(Direction.Right);
        }

        // Left
        if (index.x - 1 >= 0 && Direction.Left != exlcude)
        {
            valids.Add(index + Vector3Int.left);
            directions.Add(Direction.Left);
        }

        // Up
        if (index.y + 1 < cells.GetLength(1) && Direction.Up != exlcude)
        {
            valids.Add(index + Vector3Int.up);
            directions.Add(Direction.Up);
        }

        // Down
        if (index.y - 1 >= 0 && Direction.Down != exlcude)
        {
            valids.Add(index + Vector3Int.down);
            directions.Add(Direction.Down);
        }

        // Forward
        if (index.z + 1 < cells.GetLength(2) && Direction.Forward != exlcude)
        {
            valids.Add(index + Vector3Int.forward);
            directions.Add(Direction.Forward);
        }

        // Backward
        if (index.z - 1 >= 0 && Direction.Backward != exlcude)
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

    private List<Vector3Int> GetAllCells(Vector3 min, Vector3 max)
    {
        List<Vector3Int> surrounding = new List<Vector3Int>();

        for (float x = min.x; x <= max.x; x += 2)
        {
            for (float z = min.z; z <= max.z; z += 2)
            {
                Vector3Int index = GetIndex(new Vector3(x, min.y, z));
                surrounding.Add(index);
            }
        }

        return surrounding;
    }

    private List<Vector3Int> GetSurroundingCells(Vector3 queryPosition)
    {
        List<Vector3Int> surrounding = new List<Vector3Int>();

        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3Int index = GetIndex(queryPosition + Vector3.right * x + Vector3.forward * z);
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

                spawnedPossibilities.Add(GenerateMesh(pos, cell.PossiblePrototypes[g], scale, false));
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


    private GameObject GenerateMesh(Vector3 position, PrototypeData prototypeData, float scale = 1, bool animate = true)
    {
        if (prototypeData.MeshRot.Mesh == null)
        {
            return null;
        }

        GameObject gm = new GameObject();
        gm.AddComponent<MeshFilter>().mesh = prototypeData.MeshRot.Mesh;
        
        Material[] materials = new Material[prototypeData.MaterialIndexes.Length];
        for (int i = 0; i < prototypeData.MaterialIndexes.Length; i++)
        {
            materials[i] = mats[prototypeData.MaterialIndexes[i]];
        }
        gm.AddComponent<MeshRenderer>().materials = materials;

        gm.transform.position = position;
        gm.transform.rotation = Quaternion.Euler(0, 90 * prototypeData.MeshRot.Rot, 0);
        gm.transform.SetParent(transform, true);

        gm.transform.localScale *= scale;

        if (animate) buildingAnimator.Animate(gm);

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
public static class ArrayHelper
{
    public static bool IsInBounds<T>(this T[,,] array, int x, int y, int z)
    {
        if (x < 0 || x >= array.GetLength(0))
            return false;
        if (y < 0 || y >= array.GetLength(1))
            return false;
        if (z < 0 || z >= array.GetLength(2))
            return false;

        return true;
    }
}

