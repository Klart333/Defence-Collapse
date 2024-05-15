using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIMenuPanel : MonoBehaviour
{
    [Title("Selector")]
    [SerializeField]
    private RectTransform selector;

    [SerializeField]
    private float duration = 0.2f;

    private float startOffset;

    private void Awake()
    {
        startOffset = selector.localPosition.y;
    }

    public void HoverOption(RectTransform rectTransform)
    {
        selector.DOKill();

        selector.DOAnchorPosY(rectTransform.localPosition.y - startOffset, duration).SetEase(Ease.OutCirc);
    }

    public void StartGame()
    {
        SceneManager.LoadScene(1);
    }
}
