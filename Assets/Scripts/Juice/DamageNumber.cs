using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class DamageNumber : PooledMonoBehaviour
{
    [Title("Options")]
    [SerializeField]
    private float duration;

    [SerializeField]
    private float distance;

    private TextMeshPro text;
    public TextMeshPro Text
    {
        get
        {
            if (text == null)
            {
                text = GetComponent<TextMeshPro>();
            }

            return text;
        }
    }

    private async void OnEnable()
    {
        await UniTask.Yield();
        await UniTask.Yield();

        Vector3 endPos = transform.position + Vector3.up * distance;
        transform.DOMove(endPos, duration + 0.1f).SetEase(Ease.OutCirc);

        Color color = Text.color;
        color.a = 1f;
        Color targetColor = color;
        targetColor.a = 0;
        DOTween.To(() => color, x => { color = x; Text.color = color; }, targetColor, duration + 0.1f).SetEase(Ease.OutCirc); 
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        transform.DORewind();
    }

    public void SetDamage(DamageInstance damage)
    {
        Text.text = damage.GetTotal().ToString();
    }
}
