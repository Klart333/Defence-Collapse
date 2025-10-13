using System.Collections.Generic;
using WaveFunctionCollapse;
using Buildings.District;
using Unity.Mathematics;
using Gameplay.Upgrades;
using Buildings;

namespace Gameplay.Event
{
    public delegate void EventAction<in T0, in T1>(T0 arg0, T1 arg1);
    public delegate void EventAction<in T0>(T0 arg0);
    public delegate void EventAction();

    public static class Events
    {
        public static EventAction<WaveFunctionCollapse.Chunk> OnGroundChunkGenerated;

        public static EventAction<ICollection<ChunkIndex>> OnBuiltIndexBuilt;
        public static EventAction<ICollection<IBuildable>> OnBuildingBuilt;
        public static EventAction<BuildingType> OnBuildingClicked;

        public static EventAction OnTurnSequenceStarted;
        public static EventAction OnTurnSequenceCompleted;
        
        public static EventAction<int, int> OnTurnIncreased;
        public static EventAction OnTurnComplete;

        public static EventAction<ChunkIndexEdge> OnBuiltEdgeDestroyed;
        public static EventAction<ChunkIndex> OnBuiltIndexDestroyed;
        
        public static EventAction<DistrictData> OnCapitolDestroyed;
        public static EventAction OnFinalBossDeafeted;

        public static EventAction<TowerData> OnDistrictClicked;
        public static EventAction<TowerData> OnDistrictBuilt;
        public static EventAction<TowerData> OnDistrictUnlocked;

        public static EventAction<UpgradeCardData.UpgradeCardInstance> OnUpgradeCardPicked;
        public static EventAction OnUpgradeCardsDisplayed;

        public static EventAction OnGameReset;

        public static EventAction OnDistrictLimitReached;
        public static EventAction OnDistrictLimitUnReached;
    }

    public static class ECSEvents
    {
        public static EventAction<float3> OnLootSpawn; // TODO: Remove
    }

    public static class UIEvents
    {
        public static EventAction<IDraggable> OnBeginDrag;
        public static EventAction<IDraggable> OnEndDrag;

        public static EventAction OnFocusChanged;
    }
}