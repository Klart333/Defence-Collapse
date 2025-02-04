using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public static class Events
{
    public static UnityAction<BuildingType> OnBuildingClicked;
    public static UnityAction<BuildingType> OnBuildingPurchased; 
    public static UnityAction<IEnumerable<IBuildable>> OnBuildingBuilt; 
    public static UnityAction OnBuildingCanceled; 

    public static UnityAction OnWaveClicked;
    public static UnityAction OnWaveStarted;

    public static UnityAction<Building> OnBuildingDestroyed;
    public static UnityAction<Building> OnBuildingRepaired;

    public static UnityAction<Vector3, Vector3> OnEnemyPathUpdated;
    public static UnityAction<Vector3> OnTownDestroyed;
    public static UnityAction<DistrictType> OnDistrictClicked;
}

public static class UIEvents
{
    public static UnityAction<UIEffectDisplay> OnBeginDrag;
    public static UnityAction<UIEffectDisplay> OnEndDrag;
}