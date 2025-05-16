using System;
using Buildings.District;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UIDistrictFlipButton : MonoBehaviour
    {
        private static readonly int Interactable = Animator.StringToHash("Interactable");
        private static readonly int Flip = Animator.StringToHash("Flip");
        
        [SerializeField]
        private Button districtButton;
        
        [SerializeField]
        private Animator animator;

        [SerializeField]
        private AnimationClip unFlipAnimation;
        
        private DistrictPlacer districtPlacer;

        private void OnEnable()
        {
            districtPlacer = FindFirstObjectByType<DistrictPlacer>();
            districtPlacer.OnPlacingCanceled += OnPlacingCanceled;
        }

        private void OnDisable()
        {
            districtPlacer.OnPlacingCanceled -= OnPlacingCanceled;
        }

        public void OnButtonClicked()
        {
            districtButton.interactable = false;
            animator.SetBool(Interactable, false);
            animator.SetTrigger(Flip);
        }

        private void OnPlacingCanceled()
        {
            animator.SetBool(Interactable, true);

            SetInteractableAfterDelay().Forget();
        }

        private async UniTaskVoid SetInteractableAfterDelay()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(unFlipAnimation.length));
            districtButton.interactable = true;
        }
    }
}