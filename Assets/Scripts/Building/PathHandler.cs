using System.Collections.Generic;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using System.Linq;
using UnityEngine;

public class PathHandler : MonoBehaviour
{
    [Title("Portal")]
    [SerializeField]
    private GroundObjectData portalObjectData;

    private Node[,,] groundNodes;
    private Node[,,] nodes;

    private List<Portal> portals = new List<Portal>();
    private readonly HashSet<Vector3Int> buildingPositions = new HashSet<Vector3Int>();
    private readonly HashSet<Vector3Int> blacklistedBuildingPositions = new HashSet<Vector3Int>();

    private BuildingHandler buildingHandler;
    private WaveFunction waveFunction;

    private Vector3Int castleIndex;

    private async void OnEnable()
    {
        buildingHandler = FindAnyObjectByType<BuildingHandler>();
        waveFunction = FindAnyObjectByType<WaveFunction>();

        waveFunction.OnMapGenerated += WaveFunction_OnMapGenerated;
        Events.OnBuildingDestroyed += BuildingHandler_OnBuildingDestroyed;
        Events.OnBuildingRepaired += BuildingHandler_OnBuildingRepaired;
        portalObjectData.OnObjectSpawned += OnPortalPlaced;

        while (BuildingManager.Instance == null)
        {
            await Task.Yield(); // Dont question it man
        }
        BuildingManager.Instance.OnCastlePlaced += BuildingManager_OnCastlePlaced;
    }

    private void OnDisable()
    {
        waveFunction.OnMapGenerated -= WaveFunction_OnMapGenerated;
        Events.OnBuildingDestroyed -= BuildingHandler_OnBuildingDestroyed;
        Events.OnBuildingRepaired -= BuildingHandler_OnBuildingRepaired;
        BuildingManager.Instance.OnCastlePlaced -= BuildingManager_OnCastlePlaced;
        portalObjectData.OnObjectSpawned -= OnPortalPlaced;
    }

    private void WaveFunction_OnMapGenerated()
    {
        groundNodes = new Node[waveFunction.GridSize.x, waveFunction.GridSize.y, waveFunction.GridSize.z];
        for (int z = 0; z < waveFunction.GridSize.z; z++)
        {
            for (int y = 0; y < waveFunction.GridSize.y; y++)
            {
                for (int x = 0; x < waveFunction.GridSize.x; x++)
                {
                    Cell groundCell = waveFunction.GetCellAtIndexInverse(x, y, z);
                    if (groundCell.PossiblePrototypes[0].MeshRot.Mesh == null) // It's air
                    {
                        groundNodes[x, y, z] = new Node(false, groundCell.Position);
                    }
                    else
                    {
                        groundNodes[x, y, z] = new Node(true, groundCell.Position);
                    }
                }
            }
        }
    }

    private void OnPortalPlaced(GameObject spawnedObject) 
    {
        if (spawnedObject.TryGetComponent(out Portal portal))
        {
            portals.Add(portal);
        }
        else
        {
            Debug.LogError("Could not find portal script on spawned portal?");
        }
    }

    private void BuildingHandler_OnBuildingDestroyed(Building building)
    {
        blacklistedBuildingPositions.Add(building.Index);

        UpdateNodeMap();

        Vector3 buildingPos = building.transform.position;
        Vector3Int? target = PathFinding.BreadthFirstSearch(buildingPos, buildingPositions, nodes);

        if (target.HasValue)
        {
            Events.OnEnemyPathUpdated?.Invoke(buildingPos, BuildingManager.Instance[target.Value].Position);
        }
        else
        {
            Debug.Log("No new house found");
            Events.OnTownDestroyed?.Invoke(buildingPos);
        }
    }

    private void BuildingHandler_OnBuildingRepaired(Building building)
    {
        blacklistedBuildingPositions.Remove(building.Index);
    }

    private void BuildingManager_OnCastlePlaced(Vector3Int index)
    {
        castleIndex = index;
        //MoneyManager.Instance.AddBuildable(BuildingType.Path, FindShortestPath());
    }

    public List<(Vector3, Vector3)> GetEnemySpawnPoints()
    {
        if (portals.All(x => x.Locked))
        {
            return null;
        }
        UpdateNodeMap();

        List<(Vector3, Vector3)> spawnPoints = new List<(Vector3, Vector3)>();
        for (int i = 0; i < portals.Count; i++)
        {
            if (portals[i].Locked)
            {
                continue;
            }

            Vector3 portalPos = portals[i].transform.position;
            Vector3Int? target = PathFinding.BreadthFirstSearch(portalPos, buildingPositions, nodes); // SHOULD USE ENEMY NAV AGENT PATHFINDING

            if (target.HasValue)
            {
                spawnPoints.Add((portalPos + new Vector3(1, 0, 1), BuildingManager.Instance[target.Value].Position));
                Debug.Log("Found target building at: " + BuildingManager.Instance[target.Value].Position, portals[i]);
            }
            else
            {
                //Debug.LogError("Could not find target building from: ", portals[i]);
            }
        }

        return spawnPoints;
    } 

    private void UpdateNodeMap()
    {
        nodes = new Node[BuildingManager.Instance.Cells.GetLength(0), BuildingManager.Instance.Cells.GetLength(1), BuildingManager.Instance.Cells.GetLength(2)];

        for (int z = 0; z < nodes.GetLength(2); z++)
        {
            for (int y = 0; y < nodes.GetLength(1); y++)
            {
                for (int x = 0; x < nodes.GetLength(0); x++)
                {
                    Cell buildingCell = BuildingManager.Instance[new Vector3Int(x, y, z)];
                    bool walkable = false;
                    if (buildingCell.Buildable || (buildingCell.Collapsed && buildingCell.PossiblePrototypes[0].MeshRot.Mesh != null))
                    {
                        walkable = true;
                    }

                    nodes[x, y, z] = new Node(walkable, buildingCell.Position);
                }
            }
        }

        buildingPositions.Clear();
        foreach (var buildings in buildingHandler.BuildingGroups.Values)
        {
            buildingPositions.AddRange(buildings.Select(x => x.Index).Where(x => !blacklistedBuildingPositions.Contains(x))); // Questionable
        }
    }
}