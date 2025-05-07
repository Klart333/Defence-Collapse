using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System;
using System.Threading.Tasks;
using Sirenix.OdinInspector;

namespace Pathfinding
{
    public class PathTarget : MonoBehaviour, IPathTarget
    {
        private enum PathTargetType
        {
            Blocker,
            Target,
            Path,
        }

        public event Action OnIndexerRebuild;

        [Title("Path Target")]
        [SerializeField]
        private PathTargetType targetType;

        [Title("References")]
        [SerializeField]
        private Indexer indexer;

        public byte Importance { get; set; } = 1;
        public List<PathIndex> TargetIndexes => indexer.Indexes;

        private void OnValidate()
        {
            if (indexer == null)
            {
                indexer = GetComponent<Indexer>();
            }
        }

        private void OnEnable()
        {
            indexer ??= GetComponent<Indexer>();
            indexer.OnRebuilt += OnRebuilt;
            
            if (PathManager.Instance == null)
            {
                WaitToRegister().Forget();
            }
            else
            {
                Register();
            }
        }
        
        private void OnDisable()
        {
            Importance = 1;
            
            switch (targetType)
            {
                case PathTargetType.Blocker: PathManager.Instance.BlockerPathSet.Unregister(this); break;
                case PathTargetType.Target: PathManager.Instance.TargetPathSet.Unregister(this); break;
                case PathTargetType.Path: PathManager.Instance.PathPathSet.Unregister(this); break;
            }

            indexer.OnRebuilt -= OnRebuilt;
        }

        private async UniTaskVoid WaitToRegister()
        {
            await UniTask.WaitUntil(() => PathManager.Instance != null);
            Register();
        }

        private void Register()
        {
            switch (targetType)
            {
                case PathTargetType.Blocker: PathManager.Instance.BlockerPathSet.Register(this); break;
                case PathTargetType.Target: PathManager.Instance.TargetPathSet.Register(this); break;
                case PathTargetType.Path: PathManager.Instance.PathPathSet.Register(this); break;
            }
        }

        private void OnRebuilt()
        {
            OnIndexerRebuild?.Invoke();
        }
    }
}
