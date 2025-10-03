using System.Collections.Generic;
using UnityEngine.Serialization;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Buildings.District.UI
{
    public class UIDistrictDisplayHandler : MonoBehaviour 
    {
        [FormerlySerializedAs("displayPrefab")]
        [Title("Setup")]
        [SerializeField]
        private UIDistrictDisplay clickHandlerPrefab;

        [SerializeField]
        private RectTransform displayParent;
     
        [Title("References")]
        [SerializeField]
        private DistrictHandler districtHandler;
        
        private List<UIDistrictDisplay> spawnedDisplays = new List<UIDistrictDisplay>();

        private void OnEnable()
        {
            districtHandler.OnDistrictDisplayed += Display;
        }

        private void OnDisable()
        {
            districtHandler.OnDistrictDisplayed -= Display;
        }

        private void Display(DistrictData districtData)
        {
            UIDistrictDisplay clickHandler = clickHandlerPrefab.Get<UIDistrictDisplay>(displayParent);
            clickHandler.Display(districtData);
            spawnedDisplays.Add(clickHandler);
        }

        private void HideDisplays()
        {
            for (int i = 0; i < spawnedDisplays.Count; i++)
            {
                spawnedDisplays[i].Hide();
            }
            
            spawnedDisplays.Clear();
        }
    }
}