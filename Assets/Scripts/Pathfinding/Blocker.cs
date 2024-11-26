using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Blocker : MonoBehaviour
{
    public event Action OnBlockerRebuilt;

    [SerializeField]
    private Indexer indexer;

    public List<int> BlockedIndexes => indexer.Indexes;

    private async void OnEnable()
    {
        indexer ??= GetComponent<Indexer>();

        indexer.OnRebuilt += OnRebuilt;

        await UniTask.WaitUntil(() => PathManager.Instance != null);
        PathManager.Instance.RegisterBlocker(this);
    }

    private void OnDisable()
    {
        PathManager.Instance.UnregisterBlocker(this);
        indexer.OnRebuilt -= OnRebuilt;
    }

    private void OnRebuilt()
    {
        OnBlockerRebuilt?.Invoke();
    }
}
