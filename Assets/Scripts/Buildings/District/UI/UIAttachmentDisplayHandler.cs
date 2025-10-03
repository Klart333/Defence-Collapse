using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using Gameplay.Event;
using UnityEngine;

namespace Buildings.District.UI
{
    public class UIAttachmentDisplayHandler : MonoBehaviour
    {
        [Title("Setup")]
        [SerializeField]
        private UIAttachmentDisplay attachmentDisplayPrefab;

        [SerializeField]
        private RectTransform attachmentDisplayContainer;
        
        private List<UIAttachmentDisplay> spawnedDisplays = new List<UIAttachmentDisplay>();
        
        private DistrictData displayingDistrict;

        private void OnDisable()
        {
            Hide();
            
            Events.OnTurnComplete -= UpdateInformation;
        }

        private void UpdateInformation()
        {
            Hide();
            UpdateDisplay();
        }

        public void DisplayInformation(DistrictData districtData)
        {
            displayingDistrict = districtData;
            Events.OnTurnComplete += UpdateInformation;

            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            DistrictAttachmentData[] datas = displayingDistrict.GetAttachmentDatas();

            datas.Sort();

            for (int i = 0; i < datas.Length; i++)
            {
                UIAttachmentDisplay spawned = attachmentDisplayPrefab.Get<UIAttachmentDisplay>(attachmentDisplayContainer);
                spawned.DisplayAttachementData(displayingDistrict, datas[i]);
                spawned.transform.SetSiblingIndex(i + 1);
                spawnedDisplays.Add(spawned);
            }
        }

        public void Hide()
        {
            for (int i = 0; i < spawnedDisplays.Count; i++)
            {
                spawnedDisplays[i].gameObject.SetActive(false);
            }
            
            spawnedDisplays.Clear();
        }
    }
}