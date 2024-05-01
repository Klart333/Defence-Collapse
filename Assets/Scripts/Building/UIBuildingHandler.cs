using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIBuildingHandler : MonoBehaviour
{
    private bool gottaStartWithACastle = true;

    public void ClickBuilding()
    {
        if (gottaStartWithACastle)
        {
            Events.OnBuildingClicked.Invoke(BuildingType.Castle);
            gottaStartWithACastle = false;
            return;
        }

        Events.OnBuildingClicked.Invoke(BuildingType.Building);
    }

    public void ClickPath()
    {
        if (gottaStartWithACastle)
        {
            Events.OnBuildingClicked.Invoke(BuildingType.Castle);
            gottaStartWithACastle = false;
            return;
        }

        Events.OnBuildingClicked.Invoke(BuildingType.Path);
    }
}
