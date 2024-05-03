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
}
