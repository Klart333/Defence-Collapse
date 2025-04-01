using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using WaveFunctionCollapse;

namespace Buildings
{
    public class TreeHandler : MonoBehaviour
    {
        [SerializeField]
        private GroundGenerator groundGenerator;
        
        [SerializeField]
        private TreeGrower treeGrowerPrefab;
        
        [SerializeField]
        private Mesh[] groundTreeMeshes;
        
        private readonly List<TreeGrower> treeGrowers = new List<TreeGrower>();

        private void OnEnable()
        {
            groundGenerator.OnMapGenerated += OnMapGenerated;
            groundGenerator.OnCellCollapsed += OnCellCollapsed;
        }

        private void OnDisable()
        {
            groundGenerator.OnMapGenerated -= OnMapGenerated;
            groundGenerator.OnCellCollapsed -= OnCellCollapsed;
        }

        private void OnCellCollapsed(Cell cell)
        {
            if (!groundTreeMeshes.Contains(cell.PossiblePrototypes[0].MeshRot.Mesh)) return;
            
            TreeGrower spawned = treeGrowerPrefab.GetAtPosAndRot<TreeGrower>(cell.Position, Quaternion.identity);
            treeGrowers.Add(spawned); 
        }

        private void OnMapGenerated()
        {
            for (int i = 0; i < treeGrowers.Count; i++)
            {
                treeGrowers[i].GrowTrees().Forget(Debug.LogError);
            }
        }
    }
}
