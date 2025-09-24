using DG.Tweening.Plugins.Options;
using Sirenix.OdinInspector;
using DG.Tweening.Core;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine;

namespace UI
{
    public class UIFoldoutController : MonoBehaviour
    {
        [Title("Setup")]
        [SerializeField]
        private RectTransform foldoutTransform;

        [Title("Animation Settings", "Open")]
        [SerializeField]
        private float openDuration = 0.5f;
        
        [SerializeField]
        private Ease openEase = Ease.Linear;
        
        [SerializeField]
        private float closeDuration = 0.5f;
        
        [SerializeField]
        private Ease closeEase = Ease.Linear;
        
        public bool IsOpen { get; private set; }

        public void ToggleOpen(bool isOpen)
        {
            IsOpen = isOpen;
            if (isOpen)
            {
                OpenFoldout();
            }
            else
            {
                CloseFoldout();
            }
        }

        private void OpenFoldout()
        {
            for (int i = 2; i < foldoutTransform.childCount - 1; i++)
            {
                Transform element = foldoutTransform.GetChild(i);
                element.DOKill();
                element.gameObject.SetActive(true);
                element.DOScaleX(1.0f, openDuration).SetEase(openEase).onUpdate = () =>
                {
                    LayoutRebuilder.MarkLayoutForRebuild(foldoutTransform);
                };
            }
        }

        private void CloseFoldout()
        {
            for (int i = 2; i < foldoutTransform.childCount - 1; i++)
            {
                Transform element = foldoutTransform.GetChild(i);
                element.DOKill();
                TweenerCore<Vector3,Vector3,VectorOptions> tween = element.DOScaleX(0.0f, closeDuration + (i - 2.0f) * 0.03f).SetEase(closeEase);
                tween.onUpdate = () =>
                {
                    LayoutRebuilder.MarkLayoutForRebuild(foldoutTransform);
                };
                tween.onComplete = () =>
                {
                    element.gameObject.SetActive(false);
                };
            }
        }
    }
}