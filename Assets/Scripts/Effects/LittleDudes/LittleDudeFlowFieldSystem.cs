using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.CompilerServices;
using WaveFunctionCollapse;
using Unity.Mathematics;
using Unity.Collections;
using Pathfinding.ECS;
using Unity.Entities;
using Pathfinding;
using Unity.Burst;
using Unity.Jobs;
using System;

namespace Effects.LittleDudes
{
    public partial class LittleDudeFlowFieldSystem : SystemBase
    {
        private BlobAssetReference<LittleDudePathChunkArray> pathChunks;
        private NativeHashMap<int2, int> chunkIndexToListIndex;

        private EntityQuery littleDudeQuery;
        private Entity blobEntity;
        
        private int arrayLength;
        private int chunkAmount;

        protected override void OnCreate()
        {
            base.OnCreate();
            
            Events.OnGroundChunkGenerated += OnGroundChunkGenerated;
            Events.OnWaveEnded += OnWaveEnded;
            InitializeFlowField();

            littleDudeQuery = SystemAPI.QueryBuilder().WithAll<LittleDudeComponent>().WithNone<Prefab>().Build();
            RequireForUpdate<LittleDudeComponent>();
        }

        private void OnGroundChunkGenerated(Chunk chunk)
        {
            chunkIndexToListIndex.Add(chunk.ChunkIndex.xz, chunkAmount);
            pathChunks = PathUtility.CreatePathChunks(++chunkAmount, chunk.ChunkIndex.xz, pathChunks);
            
            EntityManager.AddComponentData(blobEntity, new LittleDudePathBlobber
            {
                PathBlob = pathChunks,
                ChunkIndexToListIndex = chunkIndexToListIndex.AsReadOnly(),
            });
        }

        private void InitializeFlowField()
        {
            chunkIndexToListIndex = new NativeHashMap<int2, int>(200, Allocator.Persistent);
            
            chunkAmount = 0;
            chunkIndexToListIndex.Add(int2.zero, chunkAmount);    
            pathChunks = PathUtility.CreatePathChunks(++chunkAmount, int2.zero, (BlobAssetReference<LittleDudePathChunkArray>)default);
            
            blobEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(blobEntity, new LittleDudePathBlobber
            {
                PathBlob = pathChunks,
                ChunkIndexToListIndex = chunkIndexToListIndex.AsReadOnly(),
            });
        }

        protected override void OnUpdate()
        {
            UpdateFlowField();
        }

        private void UpdateFlowField()
        {
            int quadrantWidth = PathUtility.GRID_WIDTH / 2;
            int quadrantHeight = PathUtility.GRID_WIDTH / 2;
            int chunksLength = pathChunks.Value.PathChunks.Length;

            for (int i = 3; i >= 0; i--)
            {
                int start = i switch
                {
                    0 => 0,
                    1 => quadrantWidth,
                    2 => quadrantWidth * quadrantHeight * 2,
                    3 => quadrantWidth * quadrantHeight * 2 + quadrantWidth,
                    _ => throw new ArgumentOutOfRangeException()
                };
                
                var job = new LittleDudePathJob
                {
                    Start = start,
                    PathChunks = pathChunks,
                    ChunkIndexToListIndex = chunkIndexToListIndex.AsReadOnly(),
                    QuadrantHeight = quadrantWidth,
                    QuadrantWidth = quadrantWidth,
                };
                
                JobHandle handle = job.ScheduleParallelByRef(chunksLength, chunksLength, default);
                handle.Complete();
            }
        }
        
        private void OnWaveEnded()
        {
            NativeArray<Entity> dudes = littleDudeQuery.ToEntityArray(Allocator.Temp);
            EntityManager.DestroyEntity(dudes);
            dudes.Dispose();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            Events.OnGroundChunkGenerated -= OnGroundChunkGenerated;
            Events.OnWaveEnded -= OnWaveEnded;

            chunkIndexToListIndex.Dispose();
            pathChunks.Dispose();
        }
    }

    public struct LittleDudeComponent : IComponentData
    {
        public float3 HomePosition; 
    }
    
    public struct LittleDudePathChunkArray
    {
        public BlobArray<LittleDudePathChunk> PathChunks;
    }
    
    public struct LittleDudePathChunk
    {
        public BlobArray<int> TargetIndexes;
        public BlobArray<byte> Directions;
        public BlobArray<int> Distances;
        public int2 ChunkIndex;
    }
        
    [BurstCompile(FloatPrecision.Low, FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    public struct LittleDudePathJob : IJobFor
    {
        [NativeDisableParallelForRestriction]
        public BlobAssetReference<LittleDudePathChunkArray> PathChunks;

        [ReadOnly, NativeDisableContainerSafetyRestriction]
        public NativeHashMap<int2, int>.ReadOnly ChunkIndexToListIndex;

        public int Start;
        public int QuadrantWidth;
        public int QuadrantHeight;
        
        [BurstCompile]
        public void Execute(int index)
        {
            NativeArray<PathIndex> neighbours = new NativeArray<PathIndex>(8, Allocator.Temp);

            for (int y = 0; y < QuadrantHeight; y++)
            for (int x = QuadrantWidth * y; x < QuadrantWidth * (y + 1); x++)
            {
                int i = Start + y * QuadrantWidth + x;
                CalculatePathAtIndex(neighbours, ref PathChunks.Value.PathChunks[index], i);
            }

            neighbours.Dispose();
        }
        
        private void CalculatePathAtIndex(NativeArray<PathIndex> neighbours, ref LittleDudePathChunk littleDudePathChunk, int index)
        {
            int targetIndex = littleDudePathChunk.TargetIndexes[index];

            if (targetIndex != 0)
            {
                littleDudePathChunk.Distances[index] = -targetIndex * 5;
                littleDudePathChunk.Directions[index] = byte.MaxValue;
                return;
            }
            
            if (!GetClosestNeighbour(neighbours, ref littleDudePathChunk, index, out int dist, out int dirIdx)) return;
                
            littleDudePathChunk.Directions[index] = PathUtility.GetDirection(PathUtility.NeighbourDirections[dirIdx]);
            littleDudePathChunk.Distances[index] = dist;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool GetClosestNeighbour(NativeArray<PathIndex> neighbours, ref LittleDudePathChunk littleDudePathChunk, int gridIndex, out int shortestDistance, out int dirIndex)
        {
            int2 currentChunkIndex = littleDudePathChunk.ChunkIndex;
            GetNeighbours(currentChunkIndex, gridIndex, neighbours);
                
            shortestDistance = int.MaxValue;
            dirIndex = 0;
            for (int j = 0; j < 8; j++)
            {
                PathIndex neighbourIndex = neighbours[j];
                if (neighbourIndex.GridIndex == -1) continue;
                    
                ref LittleDudePathChunk neighbour = ref neighbourIndex.ChunkIndex.Equals(currentChunkIndex) 
                    ? ref littleDudePathChunk 
                    : ref PathChunks.Value.PathChunks[ChunkIndexToListIndex[neighbourIndex.ChunkIndex]];
                    
                int manhattanDist = j % 2 == 0 ? 5 : 7;
                int dist = neighbour.Distances[neighbourIndex.GridIndex] + manhattanDist;
                
                if (dist >= shortestDistance) continue;
                    
                shortestDistance = dist;
                dirIndex = j;
            }

            return shortestDistance < int.MaxValue;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetNeighbours(int2 chunkIndex, int gridIndex, NativeArray<PathIndex> array)
        {
            int x = gridIndex % PathUtility.GRID_WIDTH;
            int y = gridIndex /  PathUtility.GRID_WIDTH;

            for (int i = 0; i < 8; i++)
            {
                int2 dir = PathUtility.NeighbourDirections[i];
                int2 neighbour = new int2(x + dir.x, y + dir.y);

                array[i] = neighbour switch // Grid width / height = 16, // NO DIAGONALS BUT IT'S FINE
                {
                    {x: < 0} => ChunkIndexToListIndex.ContainsKey(new int2(chunkIndex.x - 1, chunkIndex.y)) 
                        ? new PathIndex(new int2(chunkIndex.x - 1, chunkIndex.y), PathUtility.GRID_WIDTH - 1 + y * PathUtility.GRID_WIDTH )
                        : new PathIndex(default, -1),
                    {x: >= PathUtility.GRID_WIDTH} => ChunkIndexToListIndex.ContainsKey(new int2(chunkIndex.x + 1, chunkIndex.y)) 
                        ? new PathIndex(new int2(chunkIndex.x + 1, chunkIndex.y), 0 + y * PathUtility.GRID_WIDTH )
                        : new PathIndex(default, -1),
                    {y: < 0} => ChunkIndexToListIndex.ContainsKey(new int2(chunkIndex.x, chunkIndex.y - 1)) 
                        ? new PathIndex(new int2(chunkIndex.x, chunkIndex.y - 1), x + (PathUtility.GRID_WIDTH - 1) * PathUtility.GRID_WIDTH  ) 
                        : new PathIndex(default, -1),
                    {y: >= PathUtility.GRID_WIDTH} => ChunkIndexToListIndex.ContainsKey(new int2(chunkIndex.x, chunkIndex.y + 1)) 
                        ? new PathIndex(new int2(chunkIndex.x, chunkIndex.y + 1), x + 0 * PathUtility.GRID_WIDTH ) 
                        : new PathIndex(default, -1),
                    _ => new PathIndex(chunkIndex, neighbour.x + neighbour.y * PathUtility.GRID_WIDTH),
                };
            }
        }
    }
}