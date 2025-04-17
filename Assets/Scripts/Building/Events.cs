using System.Collections.Generic;
using WaveFunctionCollapse;
using Buildings.District;
using UnityEngine.Events;

public static class Events
{
    public static UnityAction<IEnumerable<IBuildable>> OnBuildingBuilt; 
    public static UnityAction<BuildingType> OnBuildingPurchased; 
    public static UnityAction<BuildingType> OnBuildingClicked;
    public static UnityAction OnBuildingCanceled; 

    public static UnityAction OnWaveStarted;
    public static UnityAction OnWaveEnded;

    public static UnityAction<List<ChunkIndex>> OnWallsDestroyed;
    public static UnityAction<ChunkIndex> OnBuiltIndexDestroyed;
    public static UnityAction<DistrictData> OnCapitolDestroyed;

    public static UnityAction<DistrictType> OnDistrictClicked;
    
    public static UnityAction OnGameReset;
}

public static class UIEvents
{
    public static UnityAction<UIEffectDisplay> OnBeginDrag;
    public static UnityAction<UIEffectDisplay> OnEndDrag;
    
    public static UnityAction OnCapitolClicked;
}