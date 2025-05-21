using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using Gameplay.Money;
using UnityEngine;
using TMPro;

public class UIMoneyHandler : MonoBehaviour
{
    [Title("References")]
    [SerializeField]
    private TextMeshProUGUI moneyAmount;

    [Title("Settings")]
    [SerializeField]
    private float tweenDuration = 0.5f;
    
    [SerializeField]
    private Ease easeType = Ease.InOutCirc;
    
    private MoneyManager moneyManager;
    private Tween currentTween;
    
    private float money;
    
    private void Awake()
    {
        GetMoneyManager().Forget();   
    }

    private async UniTaskVoid GetMoneyManager()
    {
        moneyManager = await MoneyManager.Get();
        moneyManager.OnMoneyChanged += OnMoneyChanged;

        DisplayMoney(moneyManager.Money);
    }

    private void OnMoneyChanged(float amount)
    {
        if (currentTween != null && !currentTween.IsComplete())
        {
            currentTween.Kill();
        }

        float startMoney = money;
        currentTween = DOTween.To(() => startMoney, DisplayMoney, amount, tweenDuration);
        currentTween.SetEase(easeType);
    }

    public void DisplayMoney(float amount)
    {
        moneyAmount.text = amount.ToString("N0");
        money = amount;
    }
}
