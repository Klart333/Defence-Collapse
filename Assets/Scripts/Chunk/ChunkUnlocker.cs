using UnityEngine;
using System;
using TMPro;

namespace Chunks
{
    public class ChunkUnlocker : PooledMonoBehaviour
    {
        public event Action OnChunkUnlocked;

        [SerializeField]
        private TextMeshProUGUI costText;

        [SerializeField]
        private Transform graphicParent;
        
        public int Cost { get; set; }

        private void Awake()
        {
            GetComponent<Canvas>().worldCamera = Camera.main;
        }

        public void Unlock()
        {
            if (MoneyManager.Instance.Money < Cost)
            {
                MoneyManager.Instance.InsufficientFunds(Cost);
                return;
            }
            
            MoneyManager.Instance.RemoveMoney(Cost);
            OnChunkUnlocked?.Invoke();
            
            SetShowing(false);
        }

        public void DisplayCost()
        {
            costText.text = $"Unlock Cost: {Cost}";
        }

        public void SetShowing(bool value)
        {
            graphicParent.gameObject.SetActive(value);
        }
    }
}