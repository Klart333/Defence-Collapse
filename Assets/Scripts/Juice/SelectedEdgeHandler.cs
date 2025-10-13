using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using InputCamera;
using UnityEngine;
using Buildings;

namespace Juice
{
    public class SelectedEdgeHandler : MonoBehaviour
    {
        [Title("Setup")]
        [SerializeField]
        private TileSelector selectedTilePrefab;
        
        [SerializeField]
        private Vector3 offset = new Vector3(0, -0.1f, 0);
        
        [Title("References")]
        [SerializeField]
        private GroundGenerator groundGenerator;

        [SerializeField]
        private InputEntityWriter inputWriter;
        
        private List<TileSelector> spawnedEdges = new List<TileSelector>();

        public void SelectEdge(ChunkIndexEdge chunkIndex, TileAction tileAction, bool clearSelected = true)
        {
            Vector3 position = ChunkWaveUtility.GetPosition(chunkIndex); 

            if (clearSelected)
            {
                Hide();
            }
            
            position += offset;
            
            float angle = chunkIndex.EdgeType == EdgeType.North ? 0.0f : 90;
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
            TileSelector spawned = selectedTilePrefab.GetAtPosAndRot<TileSelector>(position, rotation);
            spawned.Display(position);
            spawned.DisplayAction(tileAction);
            spawnedEdges.Add(spawned);
            
            inputWriter.OverrideShaderMousePosition(position);
        }
        
        public void Hide()
        {
            for (int i = 0; i < spawnedEdges.Count; i++)
            {
                spawnedEdges[i].Hide();
            }
            spawnedEdges.Clear();
            
            inputWriter.DisableOverride();
        }
    }
}