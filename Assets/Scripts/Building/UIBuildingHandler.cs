using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIBuildingHandler : MonoBehaviour
{
    private bool gottaStartWithACastle = true;

    private void OnEnable()
    {
        Events.OnBuildingBuilt += DoTheThing;
    }

    private void OnDisable()
    {
        Events.OnBuildingBuilt -= DoTheThing;
    }

    private void DoTheThing(IEnumerable<IBuildable> _)
    {
        gottaStartWithACastle = false;
        Events.OnBuildingBuilt -= DoTheThing;
    }

    public void ClickBuilding()
    {
        if (gottaStartWithACastle)
        {
            Events.OnBuildingClicked?.Invoke(BuildingType.Castle);
            return;
        }

        Events.OnBuildingClicked?.Invoke(BuildingType.Building);
    }

    public void ClickPath()
    {
        if (gottaStartWithACastle)
        {
            Events.OnBuildingClicked?.Invoke(BuildingType.Castle);
            return;
        }

        Events.OnBuildingClicked?.Invoke(BuildingType.Path);
    }
}
