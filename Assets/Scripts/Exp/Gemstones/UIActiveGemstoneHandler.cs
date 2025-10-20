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
        private Transform unlockButtonParent;
        
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
            expManager.OnGemstoneDataLoaded -= OnGemstoneDataLoaded;
        }

        private async UniTaskVoid GetExpManager()
        {
            expManager = await ExpManager.Get();
            expManager.OnExpChanged += OnExpChanged;

            if (expManager.HasLoadedGemstones)
            {
                SpawnSlots(expManager.SlotsUnlocked);
                OnExpChanged();   
            }
            else
            {
                expManager.OnGemstoneDataLoaded += OnGemstoneDataLoaded;
            }
        }
        
        private void OnGemstoneDataLoaded()
        {
            expManager.OnGemstoneDataLoaded -= OnGemstoneDataLoaded;
                    
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
            unlockButtonParent.SetAsLastSibling();
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
            unlockButtonParent.SetAsLastSibling();
            
            expManager.UnlockSlot();
        }
    }
}