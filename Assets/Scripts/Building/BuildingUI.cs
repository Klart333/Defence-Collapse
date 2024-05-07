using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingUI : MonoBehaviour
{
    [SerializeField]
    private GameObject upgradablePanel;

    [SerializeField]
    private GameObject upgradePanel;

    public void OnSelected(BuildingCellInformation cellInfo)
    {
        if (cellInfo.Upgradable)
        {
            upgradablePanel.SetActive(true);
        }
    }

    public void OnDeselected()
    {
        upgradablePanel.SetActive(false);
        upgradePanel.SetActive(false);
    }

    public void OnUpgradeClicked()
    {
        upgradablePanel.SetActive(false);
        upgradePanel.SetActive(true);
    }
}
