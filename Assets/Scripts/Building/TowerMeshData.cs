using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[InlineEditor]
[CreateAssetMenu(fileName = "New Data", menuName = "Building/Mesh Data")]
public class TowerMeshData : SerializedScriptableObject
{
    [Title("Mesh Information")]
    [SerializeField]
    private Dictionary<Mesh, BuildingCellInformation> towerMeshes = new Dictionary<Mesh, BuildingCellInformation>();

    public Dictionary<Mesh, BuildingCellInformation> TowerMeshes => towerMeshes;

    public (PrototypeData, BuildingCellInformation)? GetInfo(TowerType type, List<PrototypeData> prototypes)
    {
        foreach (var kvp in towerMeshes)
        {
            if (kvp.Value.TowerType == type)
            {
                for (int i = 0; i < prototypes.Count; i++)
                {
                    if (kvp.Key == prototypes[i].MeshRot.Mesh)
                    {
                        return (prototypes[i], kvp.Value);
                    }
                }
            }
        }

        return null;
    }
}
