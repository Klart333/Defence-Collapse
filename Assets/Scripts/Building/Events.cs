using System.Collections.Generic;
using WaveFunctionCollapse;
using Buildings.District;
using Unity.Mathematics;
using UnityEngine.Events;

public static class Events
{
    public static UnityAction<IEnumerable<ChunkIndex>> OnBuiltIndexBuilt;
    public static UnityAction<ICollection<IBuildable>> OnBuildingBuilt;
    public static UnityAction<BuildingType> OnBuildingClicked;
    public static UnityAction OnBuildingCanceled; 

    public static UnityAction OnWaveStarted;
    public static UnityAction OnWaveEnded;

    public static UnityAction<List<ChunkIndex>> OnWallsDestroyed;
    public static UnityAction<ChunkIndex> OnBuiltIndexDestroyed;
    public static UnityAction<DistrictData> OnCapitolDestroyed;

    public static UnityAction<DistrictType, int> OnDistrictClicked;
    public static UnityAction<DistrictType> OnDistrictBuilt;
    public static UnityAction<TowerData> OnDistrictUnlocked;
    
    public static UnityAction OnGameReset;
}

public static class ECSEvents
{
    public static UnityAction<float3> OnLootSpawn;
}

public static class UIEvents
{
    public static UnityAction<UIEffectDisplay> OnBeginDrag;
    public static UnityAction<UIEffectDisplay> OnEndDrag;
    
    public static UnityAction OnFocusChanged;
}