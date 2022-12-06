using System;
using UnityEngine.Events;

public static class Events
{
    public static UnityAction<Building> OnBuildingClicked = delegate { };
    public static UnityAction<Building> OnBuildingPurchased = delegate { }; 
    public static UnityAction<Building> OnBuildingCanceled = delegate { }; 
    public static UnityAction<Building> OnBuildingBuilt = delegate { }; 

    public static UnityAction OnWaveClicked = delegate { };
    public static UnityAction OnWaveStarted = delegate { };
}