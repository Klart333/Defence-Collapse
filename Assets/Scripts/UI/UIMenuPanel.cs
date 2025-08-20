using Sirenix.OdinInspector;
using DG.Tweening;
using Juice;
using UnityEngine;

public class UIMenuPanel : MonoBehaviour
{
    [Title("Selector")]
    [SerializeField]
    private RectTransform selector;

    [SerializeField]
    private float duration = 0.2f;

    [SerializeField]
    private Ease ease = Ease.OutSine;

    [SerializeField]
    private float heightOffset = 10;
    
    private float startOffset;

    public void HoverOption(RectTransform rectTransform)
    {
        selector.DOKill();

        selector.DOAnchorPosY(rectTransform.anchoredPosition.y + heightOffset, duration).SetEase(ease);
    }

    public void StartGame()
    {
        SceneTransitionManager.Instance.LoadScene(1).Forget();
    }
}
