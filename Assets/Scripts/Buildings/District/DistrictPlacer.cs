using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Gameplay.Event;
using Gameplay.Money;
using UnityEngine;
using Gameplay;
using System;
using TMPro;

namespace Buildings.District
{
    public class DistrictPlacer : SerializedMonoBehaviour
    {
        public event Action OnPlacingCanceled; // Does not mean it failed

        [Title("Placing")]
        [SerializeField]
        private TileBuilder tileBuilder;
        
        [Title("District")]
        [SerializeField]
        private DistrictGenerator districtGenerator;
        
        [SerializeField]
        private BuildingManager buildingGenerator;
        
        [SerializeField]
        private GroundGenerator groundGenerator;
        
        [SerializeField]
        private DistrictHandler districtHandler;
        
        [SerializeField]
        private BuildingPlacer buildingPlacer;
        
        [Title("Cost")]
        [SerializeField]
        private TextMeshProUGUI costText;
        
        [SerializeField]
        private DistrictCostUtility districtCostData;

        [SerializeField]
        private Color affordableColor;
        
        [SerializeField]
        private Color notAffordableColor;
        
        [Title("Debug")]
        [SerializeField]
        private bool verbose = true;
        
        private MoneyManager moneyManager;
        
        private TowerData towerData;
        
        private void OnEnable()
        {
            UIEvents.OnFocusChanged += CancelPlacement;
            Events.OnDistrictClicked += DistrictClicked;
            Events.OnDistrictLimitReached += CancelPlacement;
            
            tileBuilder.OnCancelPlacement += CancelPlacement;
            
            GetMoney().Forget();
        }
        
        private async UniTaskVoid GetMoney()
        {
            moneyManager = await MoneyManager.Get();
        }

        private void OnDisable()
        {
            Events.OnDistrictLimitReached -= CancelPlacement;
            Events.OnDistrictClicked -= DistrictClicked;
            UIEvents.OnFocusChanged -= CancelPlacement;
            
            tileBuilder.OnCancelPlacement -= CancelPlacement;
            tileBuilder.OnTilePressed -= OnTilePressed;
        }
        
        private void UpdateCost()
        {
            int amount = districtHandler.GetDistrictAmount(towerData.DistrictType);
            float cost = districtCostData.GetCost(towerData.DistrictType, amount);
            if (cost <= 0) return;

            costText.text = $"{cost:N0}g";

            costText.color = moneyManager.Money >= cost ? affordableColor : notAffordableColor;
            costText.gameObject.SetActive(true);
        }

        private void DistrictClicked(TowerData towerData)
        {
            if (tileBuilder.GetIsDisplaying(out BuildingType type) && type.HasFlag(BuildingType.District))
            {
                CancelPlacement();
                return;
            }
            
            this.towerData = towerData;
            
            tileBuilder.Display(BuildingType.District, GetBuildableGroundType(), IsBuildable);
            tileBuilder.OnTilePressed += OnTilePressed;
        }

        private TileAction IsBuildable(ChunkIndex chunkIndex)
        {
            switch (tileBuilder.Tiles[chunkIndex])
            {
                case BuildingType.Barricade:
                    return TileAction.None;

                case BuildingType.District:
                    return TileAction.Sell;
            }
            
            UpdateCost();
            return TileAction.Build;
        }
        
        private GroundType GetBuildableGroundType()
        {
            return towerData.DistrictType switch
            {
                DistrictType.Mine => GroundType.Crystal,
                _ => GroundType.Grass | GroundType.Crystal
            };
        }
        
        private void OnTilePressed(ChunkIndex index, TileAction action)
        {
            switch (action)
            {
                case TileAction.None:
                    Debug.Log("Wot");
                    break;
                case TileAction.Build:
                    PlaceDistrict(index).Forget();
                    break;
                case TileAction.Sell:
                    break;
            }
        }

        private void CancelPlacement()
        {
            tileBuilder.GetIsDisplaying(out BuildingType type);
            if (!type.HasFlag(BuildingType.District))
            {
                return;
            }
            
            costText.gameObject.SetActive(false);
            tileBuilder.OnTilePressed -= OnTilePressed;

            tileBuilder.CancelDisplay();
            OnPlacingCanceled?.Invoke();
        }

        private async UniTaskVoid PlaceDistrict(ChunkIndex groundIndex)
        {
            int amount = districtHandler.GetDistrictAmount(towerData.DistrictType);
            if (!moneyManager.CanPurchase(towerData.DistrictType, amount, out float cost))
            {
                return;
            }

            tileBuilder.Tiles[groundIndex] = BuildingType.District;
            
            moneyManager.RemoveMoney(cost);
            buildingGenerator.Query(groundIndex);
            buildingGenerator.Place();

            await UniTask.WaitUntil(() => districtGenerator.GeneratorActionQueue.Count == 0);
            
            Vector3 position = ChunkWaveUtility.GetPosition(groundIndex, groundGenerator.ChunkScale, groundGenerator.ChunkWaveFunction.CellSize) + groundGenerator.ChunkWaveFunction.CellSize.XyZ(0) / 2.0f;
            districtGenerator.Query(position, towerData.DistrictHeight, towerData.PrototypeInfoData);
            HashSet<QueryChunk> chunks = new HashSet<QueryChunk>();
            foreach (QueryChunk chunk in districtGenerator.QueriedChunks)
            {
                if (chunk.PrototypeInfoData == towerData.PrototypeInfoData)
                {
                    chunks.Add(chunk);
                }
            }
            
            districtHandler.AddBuiltDistrict(chunks, towerData);
            districtGenerator.Place();
            
            costText.gameObject.SetActive(false);
            
            if (towerData.DistrictType == DistrictType.TownHall)
            {
                CancelPlacement();
            }
        }
    }
}