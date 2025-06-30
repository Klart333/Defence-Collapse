using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Entities;
using UnityEngine;
using System;

namespace Pathfinding
{
    public abstract class PathSet<T> where T : struct
    {
        public delegate ref BlobArray<T> RefFunc<in T1, out TResult>(T1 arg);
        
        protected PathSet(RefFunc<int2, BlobArray<T>> targetArray)
        {
            this.targetArray = targetArray;
        }

        protected readonly HashSet<IPathTarget> targets = new HashSet<IPathTarget>();
        protected readonly HashSet<PathIndex> TargetIndexes = new HashSet<PathIndex>();
        protected readonly RefFunc<int2, BlobArray<T>> targetArray;

        protected bool isDirty;

        public void Register(IPathTarget target)
        {
            if (!targets.Add(target))
            {
                Debug.LogError("Trying to add same target again");
                return;
            }

            target.OnIndexerRebuild += SetIsDirty;
        }

        public void Unregister(IPathTarget target)
        {
            if (targets.Remove(target))
            {
                isDirty = true;
            }
            else
            {
                Debug.LogError("Trying to remove non-registered target");
            }

            target.OnIndexerRebuild -= SetIsDirty;
        }

        private void SetIsDirty() => isDirty = true;

        public abstract void RebuildTargetHashSet();
    }

    public class BoolPathSet : PathSet<bool>
    {
        public BoolPathSet(RefFunc<int2, BlobArray<bool>> targetArray) : base(targetArray)
        {
        }

        public override void RebuildTargetHashSet()
        {
            if (!isDirty)
            {
                return;
            }

            isDirty = false;
            foreach (PathIndex index in TargetIndexes)
            {
                ref BlobArray<bool> blobArray = ref targetArray.Invoke(index.ChunkIndex);
                blobArray[index.GridIndex] = false;
            }

            TargetIndexes.Clear();
            foreach (IPathTarget target in targets)
            {
                for (int i = 0; i < target.TargetIndexes.Count; i++)
                {
                    PathIndex index = target.TargetIndexes[i];
                    if (index.GridIndex < 0 || !TargetIndexes.Add(index)) continue;

                    ref BlobArray<bool> blobArray = ref targetArray.Invoke(index.ChunkIndex);
                    blobArray[index.GridIndex] = true;
                }
            }
        }
    }

    public class IntPathSet : PathSet<int>
    {
        public IntPathSet(RefFunc<int2, BlobArray<int>> targetArray, int value) : base(targetArray)
        {
            this.value = value;
        }

        private readonly int value;

        public override void RebuildTargetHashSet()
        {
            if (!isDirty)
            {
                return;
            }

            isDirty = false;
            foreach (PathIndex index in TargetIndexes)
            {
                ref BlobArray<int> blobArray = ref targetArray.Invoke(index.ChunkIndex);
                blobArray[index.GridIndex] -= value;
            }

            TargetIndexes.Clear();
            foreach (IPathTarget target in targets)
            {
                for (int i = 0; i < target.TargetIndexes.Count; i++)
                {
                    PathIndex index = target.TargetIndexes[i];

                    if (index.GridIndex < 0 || !TargetIndexes.Add(index)) continue;

                    ref BlobArray<int> blobArray = ref targetArray.Invoke(index.ChunkIndex);
                    blobArray[index.GridIndex] += value;
                }
            }
        }
    }
    
    public class BuildingTargetPathSet : PathSet<byte>
    {
        public BuildingTargetPathSet(RefFunc<int2, BlobArray<byte>> targetArray) : base(targetArray)
        {
        }

        public override void RebuildTargetHashSet()
        {
            if (!isDirty)
            {
                return;
            }

            isDirty = false;
            foreach (PathIndex index in TargetIndexes)
            {
                ref BlobArray<byte> blobArray = ref targetArray.Invoke(index.ChunkIndex);
                blobArray[index.GridIndex] = 0;
            }

            TargetIndexes.Clear();
            foreach (IPathTarget target in targets)
            {
                for (int i = 0; i < target.TargetIndexes.Count; i++)
                {
                    PathIndex index = target.TargetIndexes[i];
                    if (index.GridIndex < 0 || !TargetIndexes.Add(index)) continue;

                    ref BlobArray<byte> blobArray = ref targetArray.Invoke(index.ChunkIndex);
                    blobArray[index.GridIndex] = target.Importance;
                }
            }
        }
    }
    
    public class ExtraDistancePathSet : PathSet<int>
    {
        private readonly int value;

        public ExtraDistancePathSet(RefFunc<int2, BlobArray<int>> targetArray, int value) : base(targetArray)
        {
            this.value = value;
        }

        public override void RebuildTargetHashSet()
        {
            if (!isDirty)
            {
                return;
            }

            isDirty = false;
            foreach (PathIndex index in TargetIndexes)
            {
                ref BlobArray<int> blobArray = ref targetArray.Invoke(index.ChunkIndex);
                blobArray[index.GridIndex] = 0;
            }

            TargetIndexes.Clear();
            foreach (IPathTarget target in targets)
            {
                for (int i = 0; i < target.TargetIndexes.Count; i++)
                {
                    PathIndex index = target.TargetIndexes[i];
                    for (int x = -1; x <= 1; x++)
                    for (int y = -1; y <= 1; y++)
                    {
                        PathIndex neighbour = new PathIndex(index.ChunkIndex, index.GridIndex + x + y * PathUtility.GRID_WIDTH);
                        if (neighbour.GridIndex is < 0 or >= PathUtility.GRID_LENGTH) continue;

                        TargetIndexes.Add(neighbour);
                        ref BlobArray<int> blobArray = ref targetArray.Invoke(neighbour.ChunkIndex);
                        blobArray[neighbour.GridIndex] += value;    
                    }
                }
            }
        }
    }

    public interface IPathTarget
    {
        public event Action OnIndexerRebuild;
        
        public byte Importance { get;}

        public List<PathIndex> TargetIndexes { get; }
    }

}