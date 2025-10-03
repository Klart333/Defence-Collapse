using FocusType = Utility.FocusType;

using Cysharp.Threading.Tasks;
using Buildings.District.UI;
using WaveFunctionCollapse;
using Buildings.District;
using UnityEngine;
using Buildings;
using Utility;

namespace UI
{
    public class UILumberMillButton : MonoBehaviour
    {
        [SerializeField]
        private TileBuilder tileBuilder;
                
        [SerializeField]
        private UIDistrictButton districtButton;

        [SerializeField]
        private UIDistrictToggleButton toggleButton;
        
        [SerializeField]
        private TowerData lumbermillData;
        
        private FocusManager focusManager;
        private Focus focus;

        private bool displaying;

        private void OnEnable()
        {
            focus = new Focus
            {
                ChangeType = FocusChangeType.Unique,
                FocusType = FocusType.Placing,
                OnFocusExit = StopDisplay
            };
            
            districtButton.Setup(FindFirstObjectByType<DistrictHandler>(), lumbermillData);
            
            GetFocus().Forget();
        }

        private async UniTaskVoid GetFocus()
        {
            focusManager = await FocusManager.Get();
        }

        public void ToggleDisplay()
        {
            displaying = !displaying;

            if (displaying)
            {
                Display();
            }
            else
            {
                StopDisplay();
            }
        }

        private void Display()
        {
            focusManager.RegisterFocus(focus);
            tileBuilder.Display(BuildingType.Lumbermill, GroundType.Tree, IsBuildable);
        }

        private TileAction IsBuildable(ChunkIndex chunkindex)
        {
            BuildingType type = tileBuilder.Tiles[chunkindex];
            return type switch
            {
                0 => TileAction.Build,
                _ => TileAction.None
            };
        }

        private void StopDisplay()
        {
            displaying = false;
            focusManager.UnregisterFocus(focus);
            
            toggleButton.Hide();
        }
    }
}