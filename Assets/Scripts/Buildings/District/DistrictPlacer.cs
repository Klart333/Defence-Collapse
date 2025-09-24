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
        
        [SerializeField]
        private DistrictPrototypeInfoUtility prototypeUtility;
        
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
        
        private DistrictType districtType;
        
        private void OnEnable()
        {
            UIEvents.OnFocusChanged += CancelPlacement;
            Events.OnDistrictClicked += DistrictClicked;
            tileBuilder.OnCancelPlacement += CancelPlacement;
            
            GetMoney().Forget();
        }
        
        private async UniTaskVoid GetMoney()
        {
            moneyManager = await MoneyManager.Get();
        }

        private void OnDisable()
        {
            tileBuilder.OnCancelPlacement -= CancelPlacement;
            Events.OnDistrictClicked -= DistrictClicked;
            tileBuilder.OnTilePressed -= OnTilePressed;
            UIEvents.OnFocusChanged -= CancelPlacement;
        }
        
        private void UpdateCost()
        {
            int amount = districtHandler.GetDistrictAmount(districtType);
            float cost = districtCostData.GetCost(districtType, amount);
            if (cost <= 0) return;

            costText.text = $"{cost:N0}g";

            costText.color = moneyManager.Money >= cost ? affordableColor : notAffordableColor;
            costText.gameObject.SetActive(true);
        }

        private void DistrictClicked(DistrictType districtType)
        {
            if (tileBuilder.GetIsDisplaying(out BuildingType type) && type.HasFlag(BuildingType.District))
            {
                CancelPlacement();
                return;
            }
            
            tileBuilder.Display(BuildingType.District, GetBuildableGroundType(), IsBuildable);
            tileBuilder.OnTilePressed += OnTilePressed;

            this.districtType = districtType;
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
            return districtType switch
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
            costText.gameObject.SetActive(false);
            tileBuilder.OnTilePressed -= OnTilePressed;

            tileBuilder.CancelDisplay();
            OnPlacingCanceled?.Invoke();
        }

        private async UniTaskVoid PlaceDistrict(ChunkIndex groundIndex)
        {
            int amount = districtHandler.GetDistrictAmount(districtType);
            if (!moneyManager.CanPurchase(districtType, amount, out float cost))
            {
                return;
            }

            tileBuilder.Tiles[groundIndex] = BuildingType.District;
            
            moneyManager.RemoveMoney(cost);

            PrototypeInfoData protInfo = prototypeUtility.GetPrototypeInfo(districtType);
            
            buildingGenerator.Query(groundIndex);
            buildingGenerator.Place();

            await UniTask.WaitUntil(() => districtGenerator.GeneratorActionQueue.Count == 0);
            
            Vector3 position = ChunkWaveUtility.GetPosition(groundIndex, groundGenerator.ChunkScale, groundGenerator.ChunkWaveFunction.CellSize) + groundGenerator.ChunkWaveFunction.CellSize.XyZ(0) / 2.0f;
            districtGenerator.Query(position, 2, protInfo);
            HashSet<QueryChunk> chunks = new HashSet<QueryChunk>();
            foreach (QueryChunk chunk in districtGenerator.QueriedChunks)
            {
                if (chunk.PrototypeInfoData == protInfo)
                {
                    chunks.Add(chunk);
                }
            }
            
            districtHandler.AddBuiltDistrict(chunks, districtType);
            districtGenerator.Place();
            
            costText.gameObject.SetActive(false);
            
            if (districtType == DistrictType.TownHall)
            {
                CancelPlacement();
            }
        }
    }
}