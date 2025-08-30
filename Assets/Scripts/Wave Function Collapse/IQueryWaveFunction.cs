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
                if (TryGetIndex(marchPos, out ChunkIndex chunk) && ChunkWaveFunction.Chunks[chunk.Index].BuiltCells[chunk.CellIndex.x, chunk.CellIndex.y, chunk.CellIndex.z])
                {
                    marchedIndex += (int)Mathf.Pow(2, i);
                }
            }
            
            return marchedIndex;
        }
        
        public bool TryGetIndex(Vector3 pos, out ChunkIndex chunkIndex)
        {
            chunkIndex = default;

            foreach (QueryMarchedChunk chunk in ChunkWaveFunction.Chunks.Values)
            {
                if (!chunk.ContainsPoint(pos, CellSize)) continue;
                
                if (TryGetIndex(pos, chunk, out var cellIndex))
                {
                    chunkIndex = new ChunkIndex(chunk.ChunkIndex, cellIndex);
                    return true;
                }
                
                return false;
            }

            return false;
        }
        
        public bool TryGetIndex(Vector3 pos, IChunk chunk, out int3 index)
        {
            pos -= chunk.Position;
            index = new int3(Utility.Math.GetMultiple(pos.x, CellSize.x), 0, Utility.Math.GetMultiple(pos.z, CellSize.z));
            if (chunk.Cells.IsInBounds(index))
            {
                return true;
            }

            return false;
        }
        
        public bool TryGetIndex(Vector3 pos, int3 chunkIndex, out int3 index)
        {
            return TryGetIndex(pos, ChunkWaveFunction.Chunks[chunkIndex], out index);
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
                if (TryGetIndex(pos, out ChunkIndex index)) 
                {
                    surrounding.Add(index);
                }
            }

            return surrounding;
        }
        
        public List<ChunkIndex> GetSurroundingMarchedIndexes(ChunkIndex queryIndex)
        {
            List<ChunkIndex> builtIndexes = new List<ChunkIndex>(4) { queryIndex };
            Vector3 queryPos = ChunkWaveFunction[queryIndex].Position;
            if (TryGetIndex(queryPos + new Vector3(-1, 0, 0).MultiplyByAxis(ChunkWaveFunction.CellSize), out ChunkIndex westCell)) 
                builtIndexes.Add(westCell);
            
            if (TryGetIndex(queryPos + new Vector3(0, 0, -1).MultiplyByAxis(ChunkWaveFunction.CellSize), out ChunkIndex southCell)) 
                builtIndexes.Add(southCell);

            if (TryGetIndex(queryPos + new Vector3(-1, 0, -1).MultiplyByAxis(ChunkWaveFunction.CellSize), out ChunkIndex southWestCell)) 
                builtIndexes.Add(southWestCell);
            
            for (int i = builtIndexes.Count - 1; i >= 0; i--)
            {
                QueryMarchedChunk chunk = ChunkWaveFunction.Chunks[builtIndexes[i].Index];
                if (!chunk.BuiltCells[builtIndexes[i].CellIndex.x, builtIndexes[i].CellIndex.y, builtIndexes[i].CellIndex.z]
                    || chunk.QueryBuiltCells.Contains(builtIndexes[i].CellIndex)) // If it's not built or only query built
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
                if (TryGetIndex(marchPos, out ChunkIndex chunk))
                {
                    result.Add(chunk);
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
        
        public static bool TryGetIndex<T>(this T queryWaveFunction, Vector3 pos, out ChunkIndex index) where T : IQueryWaveFunction
        {
            return queryWaveFunction.TryGetIndex(pos, out index);
        }
        
        public static bool TryGetIndex<T>(this T queryWaveFunction, Vector3 pos, IChunk chunk, int3 index) where T : IQueryWaveFunction
        {
            return queryWaveFunction.TryGetIndex(pos, chunk, out index);
        }
        
        public static bool TryGetIndex<T>(this T queryWaveFunction, Vector3 pos, int3 chunkIndex, out int3 index) where T : IQueryWaveFunction
        {
            return queryWaveFunction.TryGetIndex(pos, chunkIndex, out index);
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