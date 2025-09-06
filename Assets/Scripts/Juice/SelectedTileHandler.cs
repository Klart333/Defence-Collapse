using Sirenix.OdinInspector;
using UnityEngine;
using WaveFunctionCollapse;

namespace Juice
{
    public class SelectedTileHandler : MonoBehaviour
    {
        [Title("Setup")]
        [SerializeField]
        private GameObject selectedTile;
        
        [SerializeField]
        private Vector3 offset = new Vector3(0, -0.1f, 0);
        
        [Title("References")]
        [SerializeField]
        private GroundGenerator groundGenerator;

        public void SelectTile(ChunkIndex chunkIndex)
        {
            SelectTile(ChunkWaveUtility.GetPosition(chunkIndex, groundGenerator.ChunkScale, groundGenerator.ChunkWaveFunction.CellSize));
        }

        public void SelectTile(Vector3 position)
        {
            selectedTile.SetActive(true);
            selectedTile.transform.position = position + offset;
        }

        public void Hide()
        {
            selectedTile.SetActive(false);
        }
    }
}