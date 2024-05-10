using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIUpgradeDisplay : MonoBehaviour
{
    [Title("Stat Type")]
    [SerializeField]
    private LevelStat UpgradeType;

    [Title("References")]
    [SerializeField]
    private UIBuildingUpgrade buildingUpgrade;
    
    [SerializeField]
    private Image fill;

    [SerializeField]
    private TextMeshProUGUI costText;

    public void DisplayStat(float percent, float cost)
    {
        fill.fillAmount = percent;

        costText.text = Mathf.CeilToInt(cost).ToString();
    }

    public void ClickUpgrade()
    {
        if (buildingUpgrade.CanPurchase(UpgradeType))
        {
            buildingUpgrade.UpgradeStat(UpgradeType);
        }
    }
}