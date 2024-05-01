using System.Collections.Generic;
using UnityEngine;

public class PathHandler : MonoBehaviour
{
    private Node[,,] nodes;

    private List<Vector3Int> portalIndexes = new List<Vector3Int>();

    private BuildingManager buildingManager;
    private WaveFunction waveFunction;
    private MoneyManager moneyManager;

    private Vector3Int castleIndex;

    private void OnEnable()
    {
        waveFunction = FindAnyObjectByType<WaveFunction>();
        buildingManager = FindAnyObjectByType<BuildingManager>();
        moneyManager = FindAnyObjectByType<MoneyManager>();

        waveFunction.OnMapGenerated += WaveFunction_OnMapGenerated;
        buildingManager.OnCastlePlaced += BuildingManager_OnCastlePlaced;
    }

    private void OnDisable()
    {
        buildingManager.OnCastlePlaced -= BuildingManager_OnCastlePlaced;
    }

    private void WaveFunction_OnMapGenerated()
    {
        nodes = new Node[waveFunction.GridSize.x, waveFunction.GridSize.y, waveFunction.GridSize.z];

        for (int z = 0; z < waveFunction.GridSize.z; z++)
        {
            for (int y = 0; y < waveFunction.GridSize.y; y++)
            {
                for (int x = 0; x < waveFunction.GridSize.x; x++)
                {
                    Cell groundCell = waveFunction.GetCellAtIndex(z, y, x);
                    if (groundCell.PossiblePrototypes[0].MeshRot.Mesh == null) // It's air
                    {
                        nodes[x, y, z] = new Node(false, groundCell.Position);
                    }
                    else
                    {
                        nodes[x, y, z] = new Node(true, groundCell.Position);

                        if (groundCell.PossiblePrototypes[0].MeshRot.Mesh.name == "Ground_Portal") // yay hard coded names :))))
                        {
                            portalIndexes.Add(new Vector3Int(x, y, z));
                        }
                    }

                }
            }
        }
    }

    private void BuildingManager_OnCastlePlaced(Vector3Int index)
    {
        castleIndex = index;
        moneyManager.AddBuildable(BuildingType.Path, FindShortestPath());
    }

    private int FindShortestPath()
    {
        Vector3 castlePos = buildingManager.GetPos(castleIndex);
        int closest = int.MaxValue;

        for (int i = 0; i < portalIndexes.Count; i++)
        {
            List<Node> path = PathFinding.FindPath(buildingManager.GetPos(portalIndexes[i]), castlePos, nodes);
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
}