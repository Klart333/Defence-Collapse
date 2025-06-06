using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace WaveFunctionCollapse
{
    public interface IQueryWaveFunction : IChunkWaveFunction<QueryMarchedChunk>
    {
        public Dictionary<ChunkIndex, IBuildable> QuerySpawnedBuildings { get; }
        public Dictionary<ChunkIndex, IBuildable> SpawnedMeshes { get; }
        public Vector3 CellSize { get; }
        
        public void SetCell(ChunkIndex index, PrototypeData chosenPrototype, bool query = true)
        {
            ChunkWaveFunction[index] = new Cell(true, ChunkWaveFunction[index].Position, new List<PrototypeData> { chosenPrototype });
            ChunkWaveFunction.CellStack.Push(index);

            IBuildable spawned = GenerateMesh(ChunkWaveFunction[index].Position, index, chosenPrototype);
            
            if (query)
            {
                QuerySpawnedBuildings.TryAdd(index, spawned);
                return;
            }

            spawned.ToggleIsBuildableVisual(false, false);
            SpawnedMeshes[index] = spawned;
        }

        public IBuildable GenerateMesh(Vector3 position, ChunkIndex index, PrototypeData prototypeData, bool animate = false);
        
        public HashSet<QueryMarchedChunk> GetChunks(IEnumerable<ChunkIndex> cellsToCollapse)
        {
            HashSet<QueryMarchedChunk> chunks = new HashSet<QueryMarchedChunk>();
            foreach (ChunkIndex index in cellsToCollapse)
            {
                QueryMarchedChunk chunk = ChunkWaveFunction.Chunks[index.Index];
                chunks.Add(chunk);
            }

            return chunks;
        }
        
        public void MakeBuildable(IEnumerable<ChunkIndex> cellsToCollapse, PrototypeInfoData prototypeInfo) 
        {
            foreach (ChunkIndex index in cellsToCollapse)
            {
                QueryMarchedChunk chunk = ChunkWaveFunction.Chunks[index.Index];
                if (!IsBuildable(index)) continue;

                int marchedIndex = GetMarchIndex(index);
                chunk.QueryChangedCells.Add((index.CellIndex, chunk[index.CellIndex]));
                
                chunk[index.CellIndex] = new Cell(false, 
                    chunk[index.CellIndex].Position, 
                    new List<PrototypeData>(prototypeInfo.MarchingTable[marchedIndex]));

                chunk.GetAdjacentCells(index.CellIndex, out _).ForEach(x => ChunkWaveFunction.CellStack.Push(x));

                if (SpawnedMeshes.TryGetValue(index, out IBuildable buildable))
                {
                    buildable.gameObject.SetActive(false);
                    SpawnedMeshes.Remove(index);
                }
            }
        }

        public bool IsBuildable(ChunkIndex index);
        
        private int GetMarchIndex(ChunkIndex index)
        {
            int marchedIndex = 0;
            Vector3 pos = ChunkWaveFunction[index].Position + CellSize / 2.0f;
            for (int i = 0; i < 4; i++)
            {
                Vector3 marchPos = pos + new Vector3(WaveFunctionUtility.MarchDirections[i].x * CellSize.x, 0, WaveFunctionUtility.MarchDirections[i].y * CellSize.z);
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
                if (!chunk.ContainsPoint(pos, CellSize)) continue;
                
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
            int3 index = new int3(Utility.Math.GetMultiple(pos.x, CellSize.x), 0, Utility.Math.GetMultiple(pos.z, CellSize.z));
            if (chunk.Cells.IsInBounds(index))
            {
                return index;
            }

            return null;
        }
        
        public int3? GetIndex(Vector3 pos, int3 chunkIndex)
        {
            return GetIndex(pos, ChunkWaveFunction.Chunks[chunkIndex]);
        }
        
        public Vector3 GetPos(ChunkIndex index)
        {
            return ChunkWaveFunction[index].Position;
        }
        
        public List<ChunkIndex> GetCellsToCollapse(ChunkIndex queryIndex)
        {
            return GetSurroundingCells(ChunkWaveFunction[queryIndex].Position + new Vector3(CellSize.x + 0.1f, 0, CellSize.z + 0.1f));
        }

        private List<ChunkIndex> GetSurroundingCells(Vector3 queryPosition)
        {
            List<ChunkIndex> surrounding = new List<ChunkIndex>();
            for (int x = -1; x <= 1; x+=2)
            for (int z = -1; z <= 1; z+=2)
            {
                Vector3 pos = queryPosition + new Vector3(x, 0, z).MultiplyByAxis(ChunkWaveFunction.CellSize);
                ChunkIndex? index = GetIndex(pos);
                if (index.HasValue) 
                {
                    surrounding.Add(index.Value);
                }
            }

            return surrounding;
        }
        
        public List<ChunkIndex> GetSurroundingMarchedIndexes(ChunkIndex queryIndex)
        {
            List<ChunkIndex> builtIndexes = new List<ChunkIndex>(4) { queryIndex };
            Vector3 queryPos = ChunkWaveFunction[queryIndex].Position;
            ChunkIndex? westCell = GetIndex(queryPos + new Vector3(-1, 0, 0).MultiplyByAxis(ChunkWaveFunction.CellSize));
            if (westCell.HasValue) builtIndexes.Add(westCell.Value);
            
            ChunkIndex? southCell = GetIndex(queryPos + new Vector3(0, 0, -1).MultiplyByAxis(ChunkWaveFunction.CellSize));
            if (southCell.HasValue) builtIndexes.Add(southCell.Value);

            ChunkIndex? southWestCell = GetIndex(queryPos + new Vector3(-1, 0, -1).MultiplyByAxis(ChunkWaveFunction.CellSize));
            if (southWestCell.HasValue) builtIndexes.Add(southWestCell.Value);
            
            for (int i = builtIndexes.Count - 1; i >= 0; i--)
            {
                QueryMarchedChunk chunk = ChunkWaveFunction.Chunks[builtIndexes[i].Index];
                if (!chunk.BuiltCells[builtIndexes[i].CellIndex.x, builtIndexes[i].CellIndex.y, builtIndexes[i].CellIndex.z])
                {
                    builtIndexes.RemoveAt(i);
                }
            }
            
            return builtIndexes;
        }

        public List<ChunkIndex> GetCellsSurroundingMarchedIndex(ChunkIndex builtIndex)
        {
            List<ChunkIndex> result = new List<ChunkIndex>();
            Vector3 pos = ChunkWaveFunction[builtIndex].Position;
            for (int i = 0; i < 4; i++)
            {
                Vector3 marchPos = pos - new Vector3(WaveFunctionUtility.MarchDirections[i].x * CellSize.x, 0, WaveFunctionUtility.MarchDirections[i].y * CellSize.z);
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
        public static void SetCell<T>(this T queryWaveFunction, ChunkIndex index, PrototypeData prototypeData, bool query = true) where T : IQueryWaveFunction
        {
            queryWaveFunction.SetCell(index, prototypeData, query);
        }
        
        public static HashSet<QueryMarchedChunk> GetChunks<T>(this T queryWaveFunction, IEnumerable<ChunkIndex> cellsToCollapse) where T : IQueryWaveFunction
        {
            return queryWaveFunction.GetChunks(cellsToCollapse);
        }
        
        public static void MakeBuildable<T>(this T queryWaveFunction, IEnumerable<ChunkIndex> cellsToCollapse, PrototypeInfoData protInfo) where T : IQueryWaveFunction
        {
            queryWaveFunction.MakeBuildable(cellsToCollapse, protInfo);
        }
        
        public static ChunkIndex? GetIndex<T>(this T queryWaveFunction, Vector3 pos) where T : IQueryWaveFunction
        {
            return queryWaveFunction.GetIndex(pos);
        }
        
        public static int3? GetIndex<T>(this T queryWaveFunction, Vector3 pos, IChunk chunk) where T : IQueryWaveFunction
        {
            return queryWaveFunction.GetIndex(pos, chunk);
        }
        
        public static int3? GetIndex<T>(this T queryWaveFunction, Vector3 pos, int3 chunkIndex) where T : IQueryWaveFunction
        {
            return queryWaveFunction.GetIndex(pos, chunkIndex);
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