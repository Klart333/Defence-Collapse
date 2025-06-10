using Buildings;
using UnityEngine;

public class UIBuildingHandler : MonoBehaviour
{
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
