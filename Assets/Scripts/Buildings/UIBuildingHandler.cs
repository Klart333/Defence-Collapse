using System;
using System.Collections.Generic;
using Buildings;
using Buildings.District;
using Gameplay.Event;
using UI;
using UnityEngine;

public class UIBuildingHandler : MonoBehaviour
{
    [SerializeField]
    private UIDistrictUnlockHandler districtUnlockHandler;
    
    private List<UIDistrictToggleButton> districtButtons = new List<UIDistrictToggleButton>();
    private List<Action> clickActions = new List<Action>();
    
    private void OnEnable()
    {
        districtUnlockHandler.OnDistrictButtonSpawned += SubscribeToDistrictButton;
    }
    
    private void OnDisable()
    {
        for (int i = 0; i < districtButtons.Count; i++)
        {
            districtButtons[i].OnClick -= clickActions[i];
        }
        
        districtUnlockHandler.OnDistrictButtonSpawned -= SubscribeToDistrictButton;
    }

    private void SubscribeToDistrictButton(TowerData towerData, UIDistrictButton districtButton)
    {
        UIDistrictToggleButton toggleButton = districtButton.GetComponentInChildren<UIDistrictToggleButton>();
        
        DistrictType districtType = towerData.DistrictType;
        Action onClick = () =>
        {
            ClickDistrict(districtType);
        };
        toggleButton.OnClick += onClick;
                
        districtButtons.Add(toggleButton);
        clickActions.Add(onClick);
    }

    public void ClickBuilding()
    {
        Events.OnBuildingClicked?.Invoke(BuildingType.Building);
    }

    public void ClickPath()
    {
        Events.OnBuildingClicked?.Invoke(BuildingType.Barricade);
    }

    public void ClickDistrict(DistrictType district)
    {
        Events.OnDistrictClicked?.Invoke(district);
    }
}
