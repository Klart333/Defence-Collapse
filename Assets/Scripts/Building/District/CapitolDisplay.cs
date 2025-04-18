using Sirenix.OdinInspector;
using UnityEngine.UI;
using UnityEngine;
using DG.Tweening;
using Gameplay;

namespace Buildings.District
{
    public class CapitolDisplay : PooledMonoBehaviour
    {
        [Title("Visual")]
        [SerializeField]
        private Image fillImage;

        [SerializeField]
        private Button button;
        
        public CapitolHandler CapitolHandler { get; set; }
        public MineState MineState { get; private set; }

        private void OnEnable()
        {
            Events.OnWaveStarted += OnWaveStarted;
            Events.OnWaveEnded += OnWaveEnded;
            Events.OnGameReset += OnGameReset;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            
            fillImage.transform.DOComplete();
        }
        
        private void OnDestroy()
        {
            Events.OnWaveStarted -= OnWaveStarted;
            Events.OnWaveEnded -= OnWaveEnded;
            Events.OnGameReset -= OnGameReset;
        }
        
        private void OnGameReset()
        {
            Events.OnWaveStarted -= OnWaveStarted;
            Events.OnWaveEnded -= OnWaveEnded;
            Events.OnGameReset -= OnGameReset;
        }

        private void OnWaveEnded()
        {
            button.interactable = true;
        }

        private void OnWaveStarted()
        {
            button.interactable = false;
        }

        public void Setup(MineState mineState)
        {
            MineState = mineState;
            UpdateCapitolDisplay();
        }

        public void ToggleIsCapitol()
        {
            CapitolHandler.ToggleIsCapitol(this);
            UpdateCapitolDisplay();
        }

        public void UpdateCapitolDisplay()
        {
            fillImage.transform.DOKill();
            bool enable = MineState.IsCapitol;
            if (enable)
            {
                fillImage.enabled = true;
                fillImage.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutSine);
            }
            else
            {
                fillImage.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.OutSine).onComplete = () =>
                {
                    fillImage.enabled = false;
                };
            }
        }
    }
}