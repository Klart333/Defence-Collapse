using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using System.Linq;
using Buildings;
using System;

namespace WaveFunctionCollapse
{
    public class PathGenerator : MonoBehaviour, IQueryWaveFunction
    {
        public event Action<QueryMarchedChunk> OnLoaded;

        [Title("Wave Function")]
        [SerializeField]
        private ChunkWaveFunction<QueryMarchedChunk> waveFunction;

        [Title("Prototypes")]
        [SerializeField]
        private PrototypeInfoData townPrototypeInfo;

        [SerializeField]
        private ProtoypeMeshes prototypeMeshes;

        [Title("Mesh")]
        [SerializeField]
        private Path pathPrefab;

        [Title("Debug")]
        [SerializeField]
        private PooledMonoBehaviour unableToPlacePrefab;

        public Dictionary<ChunkIndex, IBuildable> QuerySpawnedBuildings { get; } = new Dictionary<ChunkIndex, IBuildable>();
        public Dictionary<ChunkIndex, IBuildable> SpawnedMeshes { get; } = new Dictionary<ChunkIndex, IBuildable>();

        private HashSet<QueryMarchedChunk> queriedChunks = new HashSet<QueryMarchedChunk>();

        private BuildingAnimator buildingAnimator;
        private GroundGenerator groundGenerator;
        private ChunkIndex queryIndex;
        private Vector3? gridScale;

        public ChunkWaveFunction<QueryMarchedChunk> ChunkWaveFunction => waveFunction;
        public PrototypeInfoData PrototypeInfo => townPrototypeInfo;
        public Vector3 ChunkScale => groundGenerator.ChunkScale;
        public bool IsGenerating { get; private set; }

        public Vector3 CellSize
        {
            get
            {
                gridScale ??= waveFunction.CellSize.MultiplyByAxis(groundGenerator.ChunkWaveFunction.CellSize);
                return gridScale.Value;
            }
        }

        private void OnEnable()
        {
            groundGenerator = FindFirstObjectByType<GroundGenerator>();
            buildingAnimator = GetComponent<BuildingAnimator>();

            groundGenerator.OnChunkGenerated += LoadCells;
        }

        private void OnDisable()
        {
            groundGenerator.OnChunkGenerated -= LoadCells;
        }

        private void LoadCells(Chunk chunk)
        {
            int3 index = chunk.ChunkIndex;
            QueryMarchedChunk queryChunk = new QueryMarchedChunk().Construct(
                Mathf.FloorToInt(chunk.Width / waveFunction.CellSize.x),
                1,
                Mathf.FloorToInt(chunk.Depth / waveFunction.CellSize.z),
                index,
                chunk.Position,
                waveFunction.GetAdjacentChunks(index).ToArray<IChunk>(),
                false) as QueryMarchedChunk;

            queryChunk.Handler = this;
            Vector3 offset = new Vector3(CellSize.x / 2.0f, 0, CellSize.z / 2.0f);
            queryChunk.LoadCells(townPrototypeInfo, CellSize, chunk, offset);
            waveFunction.LoadChunk(index, queryChunk);

            OnLoaded?.Invoke(queryChunk);
        }

        public void RemoveBuiltIndex(ChunkIndex builtIndex)
        {
            // Reset Indexes
            waveFunction.Chunks[builtIndex.Index].BuiltCells[builtIndex.CellIndex.x, builtIndex.CellIndex.y, builtIndex.CellIndex.z] = false;
            List<ChunkIndex> chunkIndexes = this.GetCellsSurroundingMarchedIndex(builtIndex);

            // Get neighbours
            HashSet<ChunkIndex> cellsToUpdate = new HashSet<ChunkIndex>(chunkIndexes);
            int3 gridSize = waveFunction.Chunks[chunkIndexes[0].Index].ChunkSize;
            for (int i = 0; i < chunkIndexes.Count; i++)
            {
                GetNeighbours(chunkIndexes[i], 1);
            }

            this.MakeBuildable(cellsToUpdate, PrototypeInfo);

            waveFunction.Propagate();

            int tries = 1000;
            IsGenerating = true;
            while (cellsToUpdate.Any(x => !waveFunction[x].Collapsed) && tries-- > 0)
            {
                ChunkIndex index = waveFunction.GetLowestEntropyIndex(cellsToUpdate);
                PrototypeData chosenPrototype = waveFunction.Collapse(waveFunction[index]);
                if (chosenPrototype.MeshRot.MeshIndex == -1)
                {
                    Cell cell = waveFunction[index];
                    cell.PossiblePrototypes = new List<PrototypeData> { PrototypeData.Empty };
                    waveFunction[index] = cell;
                    cellsToUpdate.Remove(index);
                }
                else
                {
                    this.SetCell(index, chosenPrototype);
                }

                waveFunction.Propagate();
            }

            IsGenerating = false;

            if (tries <= 0)
            {
                RevertQuery();
                return;
            }

            Place();

            void GetNeighbours(ChunkIndex chunkIndex, int depth)
            {
                List<ChunkIndex> neighbours = ChunkWaveUtility.GetNeighbouringChunkIndexes(chunkIndex, gridSize.x, gridSize.z);
                for (int j = 0; j < neighbours.Count; j++)
                {
                    if (cellsToUpdate.Contains(neighbours[j])
                        || !waveFunction.Chunks.TryGetValue(neighbours[j].Index, out QueryMarchedChunk neihbourChunk)
                        || !neihbourChunk[neighbours[j].CellIndex].Collapsed
                        || !neihbourChunk[neighbours[j].CellIndex].Buildable) continue;
                    cellsToUpdate.Add(neighbours[j]);

                    if (depth <= 0) continue;
                    GetNeighbours(neighbours[j], depth - 1);
                }
            }
        }

        #region Query & Place

        public void Place()
        {
            Events.OnBuildingBuilt?.Invoke(QuerySpawnedBuildings.Values);

            foreach (QueryMarchedChunk chunk in queriedChunks)
            {
                chunk.Place();
            }

            foreach (KeyValuePair<ChunkIndex, IBuildable> item in QuerySpawnedBuildings)
            {
                SpawnedMeshes.Add(item.Key, item.Value);
            }

            foreach (IBuildable item in QuerySpawnedBuildings.Values)
            {
                item.ToggleIsBuildableVisual(false, false);
            }

            QuerySpawnedBuildings.Clear();
        }

        public void RevertQuery()
        {
            if (QuerySpawnedBuildings.Count == 0)
            {
                return;
            }

            foreach (QueryMarchedChunk chunk in queriedChunks)
            {
                chunk.RevertQuery();
            }

            foreach (IBuildable item in QuerySpawnedBuildings.Values)
            {
                item.gameObject.SetActive(false);
            }

            QuerySpawnedBuildings.Clear();
        }

        public Dictionary<ChunkIndex, IBuildable> Query(ChunkIndex queryIndex)
        {
            RevertQuery();

            List<ChunkIndex> cellsToCollapse = this.GetCellsToCollapse(queryIndex);
            if (cellsToCollapse.Count <= 0) return QuerySpawnedBuildings;

            queriedChunks = this.GetChunks(cellsToCollapse);
            waveFunction.Chunks[queryIndex.Index].SetBuiltCells(queryIndex.CellIndex);
            this.MakeBuildable(cellsToCollapse, PrototypeInfo);

            waveFunction.Propagate();

            IsGenerating = true;
            int tries = 1000;
            while (cellsToCollapse.Any(x => !waveFunction[x].Collapsed) && tries-- > 0)
            {
                ChunkIndex index = waveFunction.GetLowestEntropyIndex(cellsToCollapse);
                PrototypeData chosenPrototype = waveFunction.Collapse(waveFunction[index]);
                this.SetCell(index, chosenPrototype);

                waveFunction.Propagate();
            }

            IsGenerating = false;

            if (tries <= 0)
            {
                Debug.LogError("Ran out of attempts to collapse");
            }

            return QuerySpawnedBuildings;
        }

        public IBuildable GenerateMesh(Vector3 position, ChunkIndex index, PrototypeData prototypeData, bool animate = false)
        {
            Path building = pathPrefab.GetAtPosAndRot<Path>(position, Quaternion.Euler(0, 90 * prototypeData.MeshRot.Rot, 0));

            building.Setup(prototypeData, index, waveFunction.CellSize);

            if (animate) buildingAnimator.Animate(building);

            return building;
        }

        #endregion

        #region Debug

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!UnityEditor.EditorApplication.isPlaying || waveFunction.Chunks == null || waveFunction.Chunks.Count == 0)
            {
                return;
            }

            foreach (QueryMarchedChunk chunk in waveFunction.Chunks.Values)
            {
                for (int y = 0; y < chunk.Cells.GetLength(2); y++)
                {
                    for (int x = 0; x < chunk.Cells.GetLength(0); x++)
                    {
                        Vector3 pos = chunk.Cells[x, 0, y].Position;
                        Gizmos.color = chunk.BuiltCells[x, 0, y]
                            ? Color.magenta
                            : chunk.Cells[x, 0, y].Collapsed
                                ? Color.blue
                                : Color.white;
                        Gizmos.DrawWireCube(pos, CellSize * 0.9f);
                    }
                }
            }

        }
#endif
        #endregion
    }
}