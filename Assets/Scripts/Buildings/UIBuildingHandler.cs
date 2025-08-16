using System.Collections.Generic;
using Buildings.District;
using UnityEngine;
using Buildings;
using System;
using UI;

public class UIBuildingHandler : MonoBehaviour
{
    [SerializeField]
    private UIDistrictUnlockHandler districtUnlockHandler;
    
    private List<UIDistrictFlipButton> districtButtons = new List<UIDistrictFlipButton>();
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
        UIDistrictFlipButton flipButton = districtButton.GetComponentInChildren<UIDistrictFlipButton>();
        
        int index = (int)towerData.DistrictType;
        Action onClick = () =>
        {
            ClickDistrict(index);
        };
        flipButton.OnClick += onClick;
                
        districtButtons.Add(flipButton);
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

    public void ClickDistrict(int type)
    {
        if (type >= (int)DistrictType.TownHall) type++; // Don't worry about it
        
        DistrictType district = (DistrictType)type;
        Events.OnDistrictClicked?.Invoke(district, district switch
        {
            DistrictType.Bomb => 3,
            DistrictType.Church => 3,
            DistrictType.Lightning => 1,
            DistrictType.Mine => 1,
            _ => 2, 
        });
    }
}
