using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Pathfinding.ECS
{
    public struct PathBlobber : IComponentData
    {
        public BlobAssetReference<PathChunkArray> PathBlob;
        public NativeHashMap<int2, int>.ReadOnly ChunkIndexToListIndex;
    }
    
    public struct LittleDudePathBlobber : IComponentData
    {
        public BlobAssetReference<Effects.LittleDudes.LittleDudePathChunkArray> PathBlob;
        public NativeHashMap<int2, int>.ReadOnly ChunkIndexToListIndex;
    }
}