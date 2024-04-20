using System;
using UnityEngine;
using UnityEngine.Events;

public static class Events
{
    public static UnityAction<Building> OnBuildingClicked {get; internal set; }
    public static UnityAction<Building> OnBuildingPurchased = delegate { }; 
    public static UnityAction<Building> OnBuildingBuilt = delegate { }; 
    public static UnityAction OnBuildingCanceled = delegate { }; 

    public static UnityAction OnWaveClicked = delegate { };
    public static UnityAction OnWaveStarted = delegate { };

    public static Action OnPathClicked = delegate { };
    public static Action OnPathPurchased = delegate { };
}

public static class GameEvents
{
    public static UnityAction<Vector3> OnEnemyPathUpdated = delegate { };

    public static UnityAction<Vector3> OnFightStarted = delegate { };
    public static UnityAction<Vector3> OnFightEnded = delegate { };
}