using System;
using System.Collections.Generic;
using UnityEngine;

public class PathSet
{
    private readonly HashSet<IPathTarget> targets = new HashSet<IPathTarget>();

    public List<int> TargetIndexes = new List<int>();

    public void Register(IPathTarget target)
    {
        if (!targets.Add(target))
        {
            Debug.LogError("Trying to add same target again");
            return;
        }

        target.OnIndexerRebuild += RebuildTargetHashSet;
    }


    public void Unregister(IPathTarget target)
    {
        if (!targets.Remove(target))
        {
            Debug.LogError("Trying to remove non-registered target");
        }

        target.OnIndexerRebuild -= RebuildTargetHashSet;
    }

    private void RebuildTargetHashSet()
    {
        TargetIndexes.Clear();
        foreach (var target in targets)
        {
            for (int i = 0; i < target.TargetIndexes.Count; i++)
            {
                TargetIndexes.Add(target.TargetIndexes[i]);
            }
        }
    }
}

public interface IPathTarget
{
    public event Action OnIndexerRebuild;

    public List<int> TargetIndexes { get; }
}
