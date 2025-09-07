using System;
using Buildings.District;
using Cysharp.Threading.Tasks;
using Gameplay.Event;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay
{
    public class GameStateHandler : MonoBehaviour
    {
        [Title("Game over")]
        [SerializeField]
        private GameObject gameOverPanelPrefab;

        [SerializeField]
        private float gameOverDelay;
        
        private void OnEnable()
        {
            Events.OnCapitolDestroyed += OnCapitolDestroyed;
        }

        private void OnDisable()
        {
            Events.OnCapitolDestroyed -= OnCapitolDestroyed;
        }

        private void OnCapitolDestroyed(DistrictData destroyedDistrict)
        {
            InstantiateAfterDelay().Forget();
        }

        private async UniTaskVoid InstantiateAfterDelay()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(gameOverDelay));
            Instantiate(gameOverPanelPrefab);
        }
    }
}