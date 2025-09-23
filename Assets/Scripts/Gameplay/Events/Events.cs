using System.Collections.Generic;
using WaveFunctionCollapse;
using UnityEngine.Events;
using Buildings.District;
using Unity.Mathematics;
using Gameplay.Upgrades;
using Buildings;

namespace Gameplay.Event
{
    public static class Events
    {
        public static UnityAction<WaveFunctionCollapse.Chunk> OnGroundChunkGenerated;

        public static UnityAction<IEnumerable<ChunkIndex>> OnBuiltIndexBuilt;
        public static UnityAction<ICollection<IBuildable>> OnBuildingBuilt;
        public static UnityAction<BuildingType> OnBuildingClicked;

        public static UnityAction<int, int> OnTurnIncreased;
        public static UnityAction OnTurnComplete;

        public static UnityAction<List<ChunkIndex>> OnWallsDestroyed;
        public static UnityAction<ChunkIndex> OnBuiltIndexDestroyed;
        public static UnityAction<DistrictData> OnCapitolDestroyed;

        public static UnityAction<DistrictType> OnDistrictClicked;
        public static UnityAction<DistrictType> OnDistrictBuilt;
        public static UnityAction<TowerData> OnDistrictUnlocked;

        public static UnityAction OnUpgradeCardsDisplayed;
        public static UnityAction<UpgradeCardData.UpgradeCardInstance> OnUpgradeCardPicked;

        public static UnityAction OnGameReset;
    }

    public static class ECSEvents
    {
        public static UnityAction<float3> OnLootSpawn; // TODO: Remove
    }

    public static class UIEvents
    {
        public static UnityAction<IDraggable> OnBeginDrag;
        public static UnityAction<IDraggable> OnEndDrag;

        public static UnityAction OnFocusChanged;
    }
}