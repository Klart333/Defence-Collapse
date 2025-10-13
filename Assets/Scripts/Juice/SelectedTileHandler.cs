using System.Collections.Generic;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using DG.Tweening;
using InputCamera;
using UnityEngine;
using Variables;
using Buildings;

namespace Juice
{
    public class SelectedTileHandler : MonoBehaviour
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
        
        private List<TileSelector> spawnedTiles = new List<TileSelector>();
        
        public void SelectTile(ChunkIndex chunkIndex, TileAction tileAction)
        {
            Vector3 pos = ChunkWaveUtility.GetPosition(chunkIndex, groundGenerator.ChunkScale, groundGenerator.ChunkWaveFunction.CellSize); 
            SelectTile(pos + groundGenerator.ChunkWaveFunction.CellSize.XyZ(0) / 2.0f, tileAction);
        }

        public void SelectTile(Vector3 position, TileAction tileAction, bool clearSelected = true)
        {
            if (clearSelected)
            {
                Hide();
            }
            
            position += offset;
            
            TileSelector spawned = selectedTilePrefab.GetAtPosAndRot<TileSelector>(position, Quaternion.identity);
            spawned.Display(position);
            spawned.DisplayAction(tileAction);
            spawnedTiles.Add(spawned);
            
            inputWriter.OverrideShaderMousePosition(position);
        }
        
        public void SelectTiles(List<Vector3> tilePositions, TileAction tileAction)
        {
            Hide();
            
            for (int i = 0; i < tilePositions.Count; i++)
            {
                SelectTile(tilePositions[i], tileAction, false);
            }
        }
        
        public void Hide()
        {
            for (int i = 0; i < spawnedTiles.Count; i++)
            {
                spawnedTiles[i].Hide();
            }
            spawnedTiles.Clear();
            
            inputWriter.DisableOverride();
        }
    }
}