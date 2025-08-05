using UnityEngine;
using Buildings;
using System;
using UI;

public class UIBuildingHandler : MonoBehaviour
{
    [SerializeField]
    private Transform districtButtonParent;

    private UIDistrictFlipButton[] districtButtons;
    private Action[] clickActions;
    
    private void OnEnable()
    {
        districtButtons = new UIDistrictFlipButton[districtButtonParent.childCount - 1];
        clickActions = new Action[districtButtonParent.childCount - 1];
        for (int i = 1; i < districtButtonParent.childCount; i++)
        {
            if (!districtButtonParent.GetChild(i).GetChild(0).TryGetComponent(out UIDistrictFlipButton districtButton)) continue;
            
            int index = i - 1;
            Action onClick = () =>
            {
                ClickDistrict(index);
            };
            districtButton.OnClick += onClick;
                
            districtButtons[index] = districtButton;
            clickActions[index] = onClick;
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < districtButtons.Length; i++)
        {
            districtButtons[i].OnClick -= clickActions[i];
        }
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
