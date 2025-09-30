using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Gameplay.Event;
using UnityEngine;
using System;

namespace Buildings
{
    public class BuildingPlacer : MonoBehaviour
    {
        [Title("Placing")]
        [SerializeField]
        private TileBuilder tileBuilder;
        
        [SerializeField]
        private BarricadePlacer barricadePlacer;
        
        [Title("References")]
        [SerializeField]
        private DistrictGenerator districtGenerator;
        
        private BuildingHandler buildingHandler;
        private BuildingManager buildingManager;

        private void OnEnable()
        {
            buildingHandler = FindFirstObjectByType<BuildingHandler>();
            
            tileBuilder.OnCancelPlacement += OnBuildingCanceled;
            UIEvents.OnFocusChanged += OnBuildingCanceled;
            Events.OnBuildingClicked += BuildingClicked;

            GetManagers().Forget();
        }

        private async UniTaskVoid GetManagers()
        {
            buildingManager = await BuildingManager.Get();
        }

        private void OnDisable()
        {
            UIEvents.OnFocusChanged -= OnBuildingCanceled;
            Events.OnBuildingClicked -= BuildingClicked;
            tileBuilder.OnTilePressed -= OnTilePressed;
            tileBuilder.OnCancelPlacement -= OnBuildingCanceled;
        }
        
        private void OnBuildingCanceled()
        {
            tileBuilder.CancelDisplay();
            tileBuilder.OnTilePressed -= OnTilePressed;
        }

        private void BuildingClicked(BuildingType buildingType)
        {
            if (buildingType is not BuildingType.Building) return;

            if (tileBuilder.GetIsDisplaying(out BuildingType type) && type == BuildingType.Building)
            {
                OnBuildingCanceled();
                return;
            }
            
            tileBuilder.Display(BuildingType.Building, GroundType.Buildable, IsBuildable);
            tileBuilder.OnTilePressed += OnTilePressed;
        }

        private TileAction IsBuildable(ChunkIndex chunkindex)
        {
            BuildingType type = tileBuilder.Tiles[chunkindex];
            return type switch
            {
                0 => TileAction.Build,
                BuildingType.Building => TileAction.Sell,
                _ => TileAction.None
            };
        }

        private void OnTilePressed(ChunkIndex index, TileAction action)
        {
            switch (action)
            {
                case TileAction.None:
                    Debug.LogError("Wot");
                    break;
                case TileAction.Build:
                    PlaceBuilding(index);
                    break;
                case TileAction.Sell:
                    RemoveBuilding(index);
                    break;            
            }
        }

        private void PlaceBuilding(ChunkIndex index)
        {
            tileBuilder.Tiles[index] = BuildingType.Building;
            
            buildingManager.Query(index);
            buildingManager.Place();
        }

        private void RemoveBuilding(ChunkIndex index)
        {
            
            buildingHandler.BuildingDestroyed(index);
        }
    }

    [Flags]
    public enum BuildingType
    {
        Building = 1 << 0,
        Barricade = 2 << 0,
        District = 3 << 0,
        Lumbermill = 4 << 0,
    }
}