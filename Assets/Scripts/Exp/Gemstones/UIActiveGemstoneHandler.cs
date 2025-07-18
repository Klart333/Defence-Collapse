using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

namespace Exp.Gemstones
{
    public class UIActiveGemstoneHandler : MonoBehaviour
    {
        [Title("References")]
        [SerializeField]
        private UIGemstoneContainer containerPefab;

        [SerializeField]
        private Transform containerParent;
        
        [Title("Slot Unlock")]
        [SerializeField]
        private Button unlockButton;

        [SerializeField]
        private TextMeshProUGUI costText;

        [SerializeField]
        private int baseCost = 100;
        
        [SerializeField]
        private float multiplier = 2;
        
        private ExpManager expManager;
        
        public int Cost { get; private set; }
        
        private void Awake()
        {
            GetExpManager().Forget();
        }

        private void OnDestroy()
        {
            expManager.OnExpChanged -= OnExpChanged;
        }

        private async UniTaskVoid GetExpManager()
        {
            expManager = await ExpManager.Get();
            expManager.OnExpChanged += OnExpChanged;
            
            SpawnSlots(expManager.SlotsUnlocked);
            OnExpChanged();
        }

        private void OnExpChanged()
        {
            unlockButton.interactable = expManager.Exp >= Cost;
        }

        private void SpawnSlots(int count)
        {
            Cost = Mathf.RoundToInt(baseCost * Mathf.Pow(multiplier, count - 1));
            costText.text = $"{Cost:N0} Exp";
            
            for (int i = 0; i < count; i++)
            {
                SpawnSlot();
            }
            unlockButton.transform.SetAsLastSibling();
        }

        private void SpawnSlot()
        {
            UIGemstoneContainer spawned = Instantiate(containerPefab, containerParent);
            spawned.Index = containerParent.childCount - 2; // One extra for the unlockbutton
            spawned.transform.SetSiblingIndex(spawned.Index);
        }

        public void PurchaseNewSlot()
        {
            if (expManager.Exp < Cost)
            {
                return;
            }
            
            expManager.RemoveExp(Cost);
            Cost = Mathf.RoundToInt(Cost * multiplier);
            costText.text = $"{Cost:N0} Exp";

            SpawnSlot();
            unlockButton.transform.SetAsLastSibling();
            
            expManager.UnlockSlot();
        }
    }
}