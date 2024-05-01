using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public static class Events
{
    public static UnityAction<BuildingType> OnBuildingClicked;
    public static UnityAction<BuildingType> OnBuildingPurchased; 
    public static UnityAction<IEnumerable<IBuildable>> OnBuildingBuilt = delegate { }; 
    public static UnityAction OnBuildingCanceled = delegate { }; 

    public static UnityAction OnWaveClicked = delegate { };
    public static UnityAction OnWaveStarted = delegate { };
}

public static class GameEvents
{
    public static UnityAction<Vector3> OnEnemyPathUpdated = delegate { };
}