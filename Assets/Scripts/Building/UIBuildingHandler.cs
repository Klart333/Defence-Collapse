using System.Collections.Generic;
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
        Events.OnDistrictClicked?.Invoke((DistrictType)type);
    }
}
