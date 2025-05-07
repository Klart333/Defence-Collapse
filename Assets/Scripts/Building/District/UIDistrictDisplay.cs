using System;
using UnityEngine;

namespace Buildings.District
{
    public class UIDistrictDisplay : MonoBehaviour
    {
        private DistrictHandler handler;

        private void Awake()
        {
            handler = GetComponent<DistrictHandler>();
            handler.OnDistrictCreated += OnDistrictCreated;
        }

        private void OnDistrictCreated(DistrictData data)
        {
            data.OnClicked += OnDistrictClicked;
        }

        private void OnDistrictClicked(DistrictData data)
        {
            DistrictUpgradeManager.Instance.OpenUpgradeMenu(data).Forget();
        }
    }
}