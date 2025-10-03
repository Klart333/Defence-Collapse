using UnityEngine;

namespace Buildings.District.UI
{
    public class UIDistrictClickHandler : MonoBehaviour
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
            data.OnDisposed += DataOnOnDisposed;

            void DataOnOnDisposed()
            {
                data.OnDisposed -= DataOnOnDisposed;
                data.OnClicked -= OnDistrictClicked;
            }
        }

        private void OnDistrictClicked(DistrictData data)
        {
            DistrictUpgradeManager.Instance.OpenUpgradeMenu(data).Forget();
        }
    }
}