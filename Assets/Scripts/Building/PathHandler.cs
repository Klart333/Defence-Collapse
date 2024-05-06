using Sirenix.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PathHandler : MonoBehaviour
{
    private Node[,,] groundNodes;
    private Node[,,] nodes;

    private List<Vector3Int> portalIndexes = new List<Vector3Int>();
    private HashSet<Vector3Int> buildingPositions = new HashSet<Vector3Int>();
    private HashSet<Vector3Int> blacklistedBuildingPositions = new HashSet<Vector3Int>();

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
    }

    private void WaveFunction_OnMapGenerated()
    {
        groundNodes = new Node[waveFunction.GridSize.x, waveFunction.GridSize.y, waveFunction.GridSize.z];
        HashSet<Vector3Int> portalCoordinates = new HashSet<Vector3Int>();

        for (int z = 0; z < waveFunction.GridSize.z; z++)
        {
            for (int y = 0; y < waveFunction.GridSize.y; y++)
            {
                for (int x = 0; x < waveFunction.GridSize.x; x++)
                {
                    Cell groundCell = waveFunction.GetCellAtIndex(z, y, x);
                    if (groundCell.PossiblePrototypes[0].MeshRot.Mesh == null) // It's air
                    {
                        groundNodes[x, y, z] = new Node(false, groundCell.Position);
                    }
                    else
                    {
                        groundNodes[x, y, z] = new Node(true, groundCell.Position);

                        if (groundCell.PossiblePrototypes[0].MeshRot.Mesh.name == "Ground_Portal" && !portalCoordinates.Contains(new Vector3Int(x, y, z))) // yay hard coded names :))))
                        {
                            portalIndexes.Add(new Vector3Int(x, y, z));

                            portalCoordinates.Add(new Vector3Int(x + 1, y, z    ));
                            portalCoordinates.Add(new Vector3Int(x    , y, z + 1));
                            portalCoordinates.Add(new Vector3Int(x + 1, y, z + 1));
                        }
                    }

                }
            }
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
        MoneyManager.Instance.AddBuildable(BuildingType.Path, FindShortestPath());
    }

    private int FindShortestPath()
    {
        Vector3 castlePos = BuildingManager.Instance.GetPos(castleIndex);
        int closest = int.MaxValue;

        for (int i = 0; i < portalIndexes.Count; i++)
        {
            List<Node> path = PathFinding.FindPath(BuildingManager.Instance.GetPos(portalIndexes[i]), castlePos, groundNodes);
            if (path != null)
            {
                if (path.Count < closest)
                {
                    closest = path.Count;
                }
            }
        }

        print("Closest Path: " + closest);
        return closest;
    }

    public List<(Vector3, Vector3)> GetEnemySpawnPoints()
    {
        UpdateNodeMap();
        Vector3 castlePos = BuildingManager.Instance.GetPos(castleIndex);

        // At least one portal must connect to the castle
        for (int i = 0; i < portalIndexes.Count; i++)
        {
            Vector3 portalPos = BuildingManager.Instance.GetPos(portalIndexes[i]);
            var path = PathFinding.FindPath(portalPos, castlePos, nodes);
            
            if (path != null)
            {
                break;
            }

            if (i == portalIndexes.Count - 1)
            {
                return null; // None connected
            }
        }

        List<(Vector3, Vector3)> spawnPoints = new List<(Vector3, Vector3)>();
        for (int i = 0; i < portalIndexes.Count; i++)
        {
            Vector3 portalPos = BuildingManager.Instance.GetPos(portalIndexes[i]);
            Vector3Int? target = PathFinding.BreadthFirstSearch(portalPos, buildingPositions, nodes);

            if (target.HasValue)
            {
                spawnPoints.Add((portalPos + GetOffset(), BuildingManager.Instance[target.Value].Position));
            }
        }

        return spawnPoints;
    }

    private Vector3 GetOffset()
    {
        return new Vector3(1, 0, 1);
    }

    private void UpdateNodeMap()
    {
        nodes = new Node[waveFunction.GridSize.x, waveFunction.GridSize.y, waveFunction.GridSize.z];

        for (int z = 0; z < nodes.GetLength(2); z++)
        {
            for (int y = 0; y < nodes.GetLength(1); y++)
            {
                for (int x = 0; x < nodes.GetLength(0); x++)
                {
                    Cell buildingCell = BuildingManager.Instance[new Vector3Int(x, y, z)];
                    if (!buildingCell.Collapsed)
                    {
                        nodes[x, y, z] = new Node(false, buildingCell.Position);
                        continue;
                    }

                    if (buildingCell.PossiblePrototypes[0].MeshRot.Mesh == null) // It's air
                    {
                        nodes[x, y, z] = new Node(false, buildingCell.Position);
                        continue;
                    }

                    nodes[x, y, z] = new Node(true, buildingCell.Position);
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