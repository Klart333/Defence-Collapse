using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace WaveFunctionCollapse
{
    public interface IQueryWaveFunction : IChunkWaveFunction<QueryMarchedChunk>
    {
        public Dictionary<ChunkIndex, IBuildable> QuerySpawnedBuildings { get; }
        public Dictionary<ChunkIndex, IBuildable> SpawnedMeshes { get; }
        public Vector3 GridScale { get; }
        public PrototypeInfoData PrototypeInfo { get; }
        
        public void SetCell(ChunkIndex index, PrototypeData chosenPrototype, List<int3> queryCollapsedAir, bool query = true)
        {
            ChunkWaveFunction[index] = new Cell(true, ChunkWaveFunction[index].Position, new List<PrototypeData> { chosenPrototype });
            ChunkWaveFunction.CellStack.Push(index);

            IBuildable spawned = GenerateMesh(ChunkWaveFunction[index].Position, chosenPrototype);
            if (spawned == null) 
            {
                queryCollapsedAir.Add(index.CellIndex);
                return;
            }

            if (query)
            {
                QuerySpawnedBuildings.Add(index, spawned);
                return;
            }

            spawned.ToggleIsBuildableVisual(false, false);
            SpawnedMeshes[index] = spawned;
        }

        public IBuildable GenerateMesh(Vector3 position, PrototypeData prototypeData, bool animate = false);
        
        public List<QueryMarchedChunk> GetChunks(List<ChunkIndex> cellsToCollapse)
        {
            List<QueryMarchedChunk> chunks = new List<QueryMarchedChunk> {ChunkWaveFunction.Chunks[cellsToCollapse[0].Index]};
            for (int i = 1; i < cellsToCollapse.Count; i++)
            {
                QueryMarchedChunk chunk = ChunkWaveFunction.Chunks[cellsToCollapse[i].Index];
                if (!chunks.Contains(chunk))
                {
                    chunks.Add(chunk);
                }
            }

            return chunks;
        }
        
        public void MakeBuildable(IEnumerable<ChunkIndex> cellsToCollapse) 
        {
            foreach (ChunkIndex index in cellsToCollapse)
            {
                QueryMarchedChunk chunk = ChunkWaveFunction.Chunks[index.Index];
                if (!ChunkWaveFunction[index].Buildable) continue;

                int marchedIndex = GetMarchIndex(index);
                chunk.QueryChangedCells.Add((index.CellIndex, chunk[index.CellIndex]));
                
                chunk[index.CellIndex] = new Cell(false, 
                    chunk[index.CellIndex].Position, 
                    new List<PrototypeData>(PrototypeInfo.MarchingTable[marchedIndex]));

                chunk.GetAdjacentCells(index.CellIndex, out _).ForEach(x => ChunkWaveFunction.CellStack.Push(x));

                if (SpawnedMeshes.TryGetValue(index, out IBuildable buildable))
                {
                    buildable.gameObject.SetActive(false);
                    SpawnedMeshes.Remove(index);
                }
            }
        }
        
        private int GetMarchIndex(ChunkIndex index)
        {
            int marchedIndex = 0;
            Vector3 pos = ChunkWaveFunction[index].Position + GridScale / 2.0f;
            for (int i = 0; i < 4; i++)
            {
                Vector3 marchPos = pos + new Vector3(WaveFunctionUtility.MarchDirections[i].x * GridScale.x, 0, WaveFunctionUtility.MarchDirections[i].y * GridScale.z);
                ChunkIndex? chunk = GetIndex(marchPos);
                if (chunk.HasValue && ChunkWaveFunction.Chunks[chunk.Value.Index].BuiltCells[chunk.Value.CellIndex.x, chunk.Value.CellIndex.y, chunk.Value.CellIndex.z])
                {
                    marchedIndex += (int)Mathf.Pow(2, i);
                }
            }
            
            return marchedIndex;
        }
        
        public ChunkIndex? GetIndex(Vector3 pos)
        {
            foreach (QueryMarchedChunk chunk in ChunkWaveFunction.Chunks.Values)
            {
                if (!chunk.ContainsPoint(pos, GridScale)) continue;
                
                int3? cellIndex = GetIndex(pos, chunk);
                if (cellIndex.HasValue)
                {
                    return new ChunkIndex(chunk.ChunkIndex, cellIndex.Value);
                }
                
                return null;
            }

            return null;
        }
        
        public int3? GetIndex(Vector3 pos, IChunk chunk)
        {
            pos -= chunk.Position;
            int3 index = new int3(Math.GetMultiple(pos.x, GridScale.x), 0, Math.GetMultiple(pos.z, GridScale.z));
            if (chunk.Cells.IsInBounds(index))
            {
                return index;
            }

            return null;
        }
        
        public Vector3 GetPos(ChunkIndex index)
        {
            return ChunkWaveFunction[index].Position;
        }
        
        public List<ChunkIndex> GetCellsToCollapse(ChunkIndex queryIndex)
        {
            return GetSurroundingCells(ChunkWaveFunction[queryIndex].Position + new Vector3(GridScale.x + 0.1f, 0, GridScale.z + 0.1f));
        }

        private List<ChunkIndex> GetSurroundingCells(Vector3 queryPosition)
        {
            List<ChunkIndex> surrounding = new List<ChunkIndex>();
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    ChunkIndex? index = this.GetIndex(queryPosition + new Vector3(ChunkWaveFunction.CellSize.x * x, 0, z * ChunkWaveFunction.CellSize.z));
                    if (index.HasValue) 
                    {
                        surrounding.Add(index.Value);
                    }
                }
            }

            return surrounding;
        }
        
        public List<ChunkIndex> GetSurroundingMarchedIndexes(ChunkIndex queryIndex)
        {
            List<ChunkIndex> surroundingCells = GetSurroundingCells(GetPos(queryIndex));
            for (int i = surroundingCells.Count - 1; i >= 0; i--)
            {
                QueryMarchedChunk chunk = ChunkWaveFunction.Chunks[surroundingCells[i].Index];
                if (!chunk.BuiltCells[surroundingCells[i].CellIndex.x, surroundingCells[i].CellIndex.y, surroundingCells[i].CellIndex.z])
                {
                    surroundingCells.RemoveAt(i);
                }
            }
            
            return surroundingCells;
        }

        public List<ChunkIndex> GetCellsSurroundingMarchedIndex(ChunkIndex builtIndex)
        {
            List<ChunkIndex> result = new List<ChunkIndex>();
            Vector3 pos = ChunkWaveFunction[builtIndex].Position;
            for (int i = 0; i < 4; i++)
            {
                Vector3 marchPos = pos - new Vector3(WaveFunctionUtility.MarchDirections[i].x * GridScale.x, 0, WaveFunctionUtility.MarchDirections[i].y * GridScale.z);
                ChunkIndex? chunk = this.GetIndex(marchPos);
                if (chunk.HasValue)
                {
                    result.Add(chunk.Value);
                }
            }
            
            return result;
        }
    }

    public static class IQueryWaveFunctionExtensions
    {
        public static void SetCell<T>(this T queryWaveFunction, ChunkIndex index, PrototypeData prototypeData, List<int3> queryCollapsedAir, bool query = true) where T : IQueryWaveFunction
        {
            queryWaveFunction.SetCell(index, prototypeData, queryCollapsedAir, query);
        }
        
        public static List<QueryMarchedChunk> GetChunks<T>(this T queryWaveFunction, List<ChunkIndex> cellsToCollapse) where T : IQueryWaveFunction
        {
            return queryWaveFunction.GetChunks(cellsToCollapse);
        }
        
        public static void MakeBuildable<T>(this T queryWaveFunction, IEnumerable<ChunkIndex> cellsToCollapse) where T : IQueryWaveFunction
        {
            queryWaveFunction.MakeBuildable(cellsToCollapse);
        }
        
        public static ChunkIndex? GetIndex<T>(this T queryWaveFunction, Vector3 pos) where T : IQueryWaveFunction
        {
            return queryWaveFunction.GetIndex(pos);
        }
        
        public static int3? GetIndex<T>(this T queryWaveFunction, Vector3 pos, IChunk chunk) where T : IQueryWaveFunction
        {
            return queryWaveFunction.GetIndex(pos, chunk);
        }
        
        public static Vector3 GetPos<T>(this T queryWaveFunction, ChunkIndex index) where T : IQueryWaveFunction
        {
            return queryWaveFunction.GetPos(index);
        }
        
        public static List<ChunkIndex> GetSurroundingMarchedIndexes<T>(this T queryWaveFunction, ChunkIndex queryIndex) where T : IQueryWaveFunction
        {
            return queryWaveFunction.GetSurroundingMarchedIndexes(queryIndex);
        }

        public static List<ChunkIndex> GetCellsSurroundingMarchedIndex<T>(this T queryWaveFunction, ChunkIndex builtIndex) where T : IQueryWaveFunction
        {
            return queryWaveFunction.GetCellsSurroundingMarchedIndex(builtIndex);
        }
        
        public static List<ChunkIndex> GetCellsToCollapse<T>(this T queryWaveFunction, ChunkIndex queryIndex) where T : IQueryWaveFunction
        {
            return queryWaveFunction.GetCellsToCollapse(queryIndex);
        }
    }
}