using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public abstract class PathSet<T> where T : struct
{
    protected PathSet(NativeArray<T> targetArray)
    {
        this.targetArray = targetArray;
    }
    
    protected readonly HashSet<IPathTarget> targets = new HashSet<IPathTarget>();
    protected NativeArray<T> targetArray;

    protected readonly HashSet<int> TargetIndexes = new HashSet<int>();

    protected bool isDirty = false;

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
        if (!targets.Remove(target))
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
    public BoolPathSet(NativeArray<bool> targetArray) : base(targetArray)
    {
    }

    public override void RebuildTargetHashSet()
    {
        if (!isDirty)
        {
            return;
        }

        isDirty = false;
        foreach (var index in TargetIndexes)
        {
            targetArray[index] = false;
        }
        
        TargetIndexes.Clear();
        foreach (var target in targets)
        {
            for (int i = 0; i < target.TargetIndexes.Count; i++)
            {
                int index = target.TargetIndexes[i];
                TargetIndexes.Add(index);
                targetArray[index] = true;
            }
        }
    }
}
public class BytePathSet : PathSet<byte>
{
    public BytePathSet(NativeArray<byte> targetArray, int value) : base(targetArray)
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
        foreach (var index in TargetIndexes)
        {
            targetArray[index] = (byte)(targetArray[index] - value);
        }
        
        TargetIndexes.Clear();
        foreach (var target in targets)
        {
            for (int i = 0; i < target.TargetIndexes.Count; i++)
            {
                int index = target.TargetIndexes[i];
                if (TargetIndexes.Add(index))
                {
                    targetArray[index] = (byte)(targetArray[index] + value);
                }
            }
        }
    }
}

public interface IPathTarget
{
    public event Action OnIndexerRebuild;

    public List<int> TargetIndexes { get; }
}
