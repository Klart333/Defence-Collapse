using System;
using UnityEngine;

public class BuildingUI : MonoBehaviour
{
    [SerializeField]
    private Canvas canvas;

    [SerializeField]
    private GameObject upgradablePanel;

    private StupidButton button;

    private bool inMenu = false; 

    private Building building;

    public bool InMenu => inMenu || button.Hovered; 

    private void Awake()
    {
        canvas.worldCamera = Camera.main;

        building = GetComponent<Building>();
        button = upgradablePanel.GetComponentInChildren<StupidButton>();
    }

    public void Reset()
    {
        inMenu = false;

        HideAll();
    }

    private void HideAll()
    {
        canvas.gameObject.SetActive(false);
        upgradablePanel.SetActive(false);
    }

    public void Highlight(BuildingCellInformation cellInfo)
    {
        if (!cellInfo.Upgradable)
        {
            return;
        }

        canvas.gameObject.SetActive(true);
        upgradablePanel.SetActive(true);
    }

    public void Lowlight() 
    {
        inMenu = false;

        HideAll();
    }

    public void OnSelected(BuildingCellInformation cellInfo)
    {
        if (cellInfo.Upgradable)
        {
            Highlight(cellInfo);
            return;
        }

        if (cellInfo.TowerType == TowerType.None)
        {
            return;
        }

        BuildingUpgradeManager.Instance.OpenUpgradeMenu(building);
        inMenu = true;
    }

    public void OnDeselected()
    {
        inMenu = false;
    }

    public void OnUpgradeClicked()
    {
        upgradablePanel.SetActive(false);
        inMenu = true;

        BuildingUpgradeManager.Instance.OpenAdvancementMenu(building);
    }

    public void OnMenuClosed()
    {
        inMenu = false;
    }
}
