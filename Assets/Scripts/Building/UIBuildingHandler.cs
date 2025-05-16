using UnityEngine;

public class UIBuildingHandler : MonoBehaviour
{
    public void ClickBuilding()
    {
        Events.OnBuildingClicked?.Invoke(BuildingType.Building);
    }

    public void ClickPath()
    {
        Events.OnBuildingClicked?.Invoke(BuildingType.Path);
    }

    public void ClickDistrict(int type)
    {
        DistrictType district = (DistrictType)type;
        Events.OnDistrictClicked?.Invoke(district, district switch
        {
            DistrictType.Bomb => 3,
            _ => 2, 
        });
    }
}
