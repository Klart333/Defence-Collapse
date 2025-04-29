using System.Globalization;
using Gameplay.Money;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class UIMoneyHandler : MonoBehaviour
{
    [Title("References")]
    [SerializeField]
    private TextMeshProUGUI moneyAmount;

    private void Start()
    {
        MoneyManager.Instance.OnMoneyChanged += Instance_OnMoneyChanged;

        DisplayMoney(MoneyManager.Instance.Money);
    }

    private void Instance_OnMoneyChanged(float amount)
    {
        DisplayMoney(amount);
    }

    public void DisplayMoney(float amount)
    {
        moneyAmount.text = amount.ToString("N0");
    }
}
