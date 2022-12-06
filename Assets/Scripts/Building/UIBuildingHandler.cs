using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIBuildingHandler : MonoBehaviour
{
    [SerializeField]
    private Building[] towers;

    public void ClickBuilding(int index)
    {
        Events.OnBuildingClicked.Invoke(towers[index]);
    }
}
