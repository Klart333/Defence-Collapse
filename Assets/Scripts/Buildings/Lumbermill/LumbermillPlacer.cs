using System.Collections.Generic;
using Buildings.District;
using Cysharp.Threading.Tasks;
using Gameplay.Money;
using Sirenix.OdinInspector;
using TMPro;
using WaveFunctionCollapse;
using UnityEngine;

namespace Buildings.Lumbermill
{
    public class LumbermillPlacer : MonoBehaviour
    {
        [Title("References")]
        [SerializeField]
        private TileBuilder tileBuilder;
        
        [SerializeField]
        private GroundGenerator groundGenerator;

        [Title("District")]
        [SerializeField]
        private DistrictHandler districtHandler;
        
        [SerializeField]
        private DistrictGenerator districtGenerator;
        
        [SerializeField]
        private TowerData lumberMillData;

        [Title("Display")]
        [SerializeField]
        private TextMeshProUGUI costText;
        
        private MoneyManager moneyManager;
        
        private void OnEnable()
        {
            tileBuilder.OnTilePressed += OnTilePressed;

            GetMoney().Forget();
        }

        private async UniTaskVoid GetMoney()
        {
            moneyManager = await MoneyManager.Get();
        }
        
        private void OnDisable()
        {
            tileBuilder.OnTilePressed -= OnTilePressed;
        }

        private void OnTilePressed(ChunkIndex chunkIndex, TileAction tileAction)
        {
            if (tileAction == TileAction.None) return;
            
            if (!tileBuilder.GetIsDisplaying(out BuildingType buildingType) || (buildingType & BuildingType.Lumbermill) == 0)
            {
                return;
            }

            switch (tileAction)
            {
                case TileAction.Build:
                    PlaceLumbermill(chunkIndex);
                    break;
                case TileAction.Sell:
                    Debug.LogError("Stop trying to sell stuff");
                    break;
            }
        }
        
        
        private void PlaceLumbermill(ChunkIndex groundIndex)
        {
            int amount = districtHandler.GetDistrictAmount(DistrictType.Lumbermill);
            if (!moneyManager.CanPurchase(DistrictType.Lumbermill, amount, out float cost))
            {
                return;
            }

            moneyManager.RemoveMoney(cost);
            
            tileBuilder.Tiles[groundIndex] = BuildingType.Lumbermill;
            
            Vector3 position = ChunkWaveUtility.GetPosition(groundIndex, groundGenerator.ChunkScale, groundGenerator.ChunkWaveFunction.CellSize) + groundGenerator.ChunkWaveFunction.CellSize.XyZ(0) / 2.0f;
            districtGenerator.Query(position, lumberMillData.DistrictHeight, lumberMillData.PrototypeInfoData);
            HashSet<QueryChunk> chunks = new HashSet<QueryChunk>();
            foreach (QueryChunk chunk in districtGenerator.QueriedChunks)
            {
                if (chunk.PrototypeInfoData == lumberMillData.PrototypeInfoData)
                {
                    chunks.Add(chunk);
                }
            }
            districtGenerator.Place();
            
            districtHandler.AddBuiltDistrict(chunks, lumberMillData);
            
            costText.gameObject.SetActive(false);
        }
    }
}