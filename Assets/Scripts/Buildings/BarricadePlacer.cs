using Sirenix.OdinInspector;
using Gameplay.Event;
using UnityEngine;
using System;
using UnityEngine.Events;

namespace Buildings
{
    public class BarricadePlacer : MonoBehaviour
    {
        public event Action OnPlacingCanceled;
        
        [Title("References")]
        [SerializeField]
        private EdgeBuilder edgeBuilder;
        
        [Title("Events")]
        [SerializeField]
        private UnityEvent OnBarriagePlaced;
        
        private BarricadeHandler barricadeHandler;
        
        private Vector3 targetScale;
        
        private void OnEnable()
        {
            barricadeHandler = FindFirstObjectByType<BarricadeHandler>();

            Events.OnBuildingClicked += BuildingClicked;
            edgeBuilder.OnCancelPlacement += OnCanceled;
        }

        private void OnDisable()
        {
            Events.OnBuildingClicked -= BuildingClicked;
            edgeBuilder.OnCancelPlacement -= OnCanceled;
        }
        
        private void OnCanceled()
        {
            if (!edgeBuilder.GetIsDisplaying(out EdgeBuildingType type) || !type.HasFlag(EdgeBuildingType.Barricade))
            {
                return;
            }
            
            edgeBuilder.CancelDisplay();
            edgeBuilder.OnEdgePressed -= OnEdgePressed;
            
            OnPlacingCanceled?.Invoke();
        }
        
        private void BuildingClicked(BuildingType buildingType)
        {
            if (buildingType is not BuildingType.Barricade) return;

            if (edgeBuilder.GetIsDisplaying(out EdgeBuildingType type) && type == EdgeBuildingType.Barricade)
            {
                OnCanceled();
                return;
            }
            
            edgeBuilder.Display(EdgeBuildingType.Barricade, IsBuildable);
            edgeBuilder.OnEdgePressed += OnEdgePressed;
        }

        private void OnEdgePressed(ChunkIndexEdge index, TileAction action)
        {
            switch (action)
            {
                case TileAction.None:
                    break;
                case TileAction.Build:
                    PlaceBarricade(index);
                    break;
                case TileAction.Sell:
                    RemoveBarricade(index);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        }

        private TileAction IsBuildable(ChunkIndexEdge edge)
        {
            EdgeBuildingType type = edgeBuilder.Edges[edge];
            return type switch
            {
                0 => TileAction.Build,
                EdgeBuildingType.Barricade => TileAction.Sell,
                _ => TileAction.None
            };
        }
        
        private void PlaceBarricade(ChunkIndexEdge edge)
        {
            edgeBuilder.Edges[edge] = EdgeBuildingType.Barricade;
            
            barricadeHandler.PlaceBarricade(edge);
            
            OnBarriagePlaced?.Invoke();
        }

        private void RemoveBarricade(ChunkIndexEdge edge)
        {
            barricadeHandler.DestroyBarricade(edge);
        }
        
    }
}