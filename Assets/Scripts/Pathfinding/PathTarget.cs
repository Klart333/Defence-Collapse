using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System;
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

        public List<int> TargetIndexes => indexer.Indexes;

        private void OnValidate()
        {
            if (indexer == null)
            {
                indexer = GetComponent<Indexer>();
            }
        }

        private async void OnEnable()
        {
            indexer ??= GetComponent<Indexer>();

            indexer.OnRebuilt += OnRebuilt;

            await UniTask.WaitUntil(() => PathManager.Instance != null);
            switch (targetType)
            {
                case PathTargetType.Blocker:
                    PathManager.Instance.BlockerPathSet.Register(this);
                    break;
                case PathTargetType.Target:
                    PathManager.Instance.TargetPathSet.Register(this);
                    break;
                case PathTargetType.Path:
                    PathManager.Instance.PathPathSet.Register(this);
                    break;
                default:
                    break;
            }
        }

        private void OnDisable()
        {
            switch (targetType)
            {
                case PathTargetType.Blocker:
                    PathManager.Instance.BlockerPathSet.Unregister(this);
                    break;
                case PathTargetType.Target:
                    PathManager.Instance.TargetPathSet.Unregister(this);
                    break;
                case PathTargetType.Path:
                    PathManager.Instance.PathPathSet.Unregister(this);
                    break;
                default:
                    break;
            }

            indexer.OnRebuilt -= OnRebuilt;
        }

        private void OnRebuilt()
        {
            OnIndexerRebuild?.Invoke();
        }
    }
}
