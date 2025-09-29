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
    
    private bool locked;
    
    private void OnEnable()
    {
        districtUnlockHandler.OnDistrictButtonSpawned += SubscribeToDistrictButton;
        Events.OnDistrictLimitReached += LockDistricts;
        Events.OnDistrictLimitUnReached += UnlockDistricts;
    }

    private void OnDisable()
    {
        for (int i = 0; i < districtButtons.Count; i++)
        {
            districtButtons[i].OnClick -= clickActions[i];
        }
        
        districtUnlockHandler.OnDistrictButtonSpawned -= SubscribeToDistrictButton;
        
        Events.OnDistrictLimitReached -= LockDistricts;
        Events.OnDistrictLimitUnReached -= UnlockDistricts;
    }
    
    private void UnlockDistricts() => locked = false;
    private void LockDistricts() => locked = true;

    private void SubscribeToDistrictButton(TowerData towerData, UIDistrictButton districtButton)
    {
        UIDistrictToggleButton toggleButton = districtButton.GetComponentInChildren<UIDistrictToggleButton>();
        
        Action onClick = () =>
        {
            ClickDistrict(towerData);
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

    public void ClickDistrict(TowerData district)
    {
        if (locked)
        {
            return;
        }
        
        Events.OnDistrictClicked?.Invoke(district);
    }
}
