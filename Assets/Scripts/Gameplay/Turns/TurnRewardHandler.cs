using Cysharp.Threading.Tasks;
using Effects;
using Gameplay.Event;
using Sirenix.OdinInspector;
using UnityEngine;
using Saving;

namespace Gameplay.Turns
{
    public class TurnRewardHandler : MonoBehaviour
    {
        [Title("References")]
        [SerializeField]
        private TurnAmountRewardDataUtility turnAmountRewardDataUtility;

        [SerializeField]
        private TurnHandler turnHandler;
        
        [SerializeField]
        private UITurnRewardDisplay turnRewardDisplay;
        
        private TurnAmountRewardData[] activeTurnAmountRewardDatas;
        private Modifier[] activeModifiers; 
        
        private int[] turnAmountRewardLevels;
        
        private PersistantSaveManager persistantSaveManager;
        
        public int MaxTurnAmount { get; private set; }
        
        private void Awake()
        {
            GetSaveManager().Forget();
        }

        private void OnEnable()
        {
            turnHandler.OnTurnAmountChanged += OnTurnAmountChanged;
            Events.OnTurnSequenceStarted += OnTurnSequenceStarted;
            Events.OnTurnSequenceCompleted += OnTurnSequenceCompleted;
        }

        private void OnDisable()
        {
            turnHandler.OnTurnAmountChanged -= OnTurnAmountChanged;
            Events.OnTurnSequenceStarted -= OnTurnSequenceStarted;
            Events.OnTurnSequenceCompleted -= OnTurnSequenceCompleted;
        }

        private async UniTaskVoid GetSaveManager()
        {
            persistantSaveManager = await PersistantSaveManager.Get();
            LoadTurnAmountSaveData();
        }
        
        private void LoadTurnAmountSaveData()
        {
            TurnAmountSaveData saveData = persistantSaveManager.TurnAmountSaveLoad.LoadTurnData();
            MaxTurnAmount = saveData.MaxTurn;
            turnAmountRewardLevels = saveData.RewardLevels;
            
            activeTurnAmountRewardDatas = new TurnAmountRewardData[saveData.RewardsUnlocked.Length];
            for (int i = 0; i < saveData.RewardsUnlocked.Length; i++)
            {
                activeTurnAmountRewardDatas[i] = turnAmountRewardDataUtility.GetRewardData(saveData.RewardsUnlocked[i]);
            }
            
            turnHandler.SetTurnAmount(1); // eh
        }
        
        private void OnTurnAmountChanged()
        {
            turnRewardDisplay.Display(turnHandler.TurnAmount, activeTurnAmountRewardDatas, turnAmountRewardLevels);
        }
        
        private void OnTurnSequenceStarted()
        {
            activeModifiers = new Modifier[activeTurnAmountRewardDatas.Length];
            int turnAmount = turnHandler.TurnAmount;
            for (int i = 0; i < activeTurnAmountRewardDatas.Length; i++)
            {
                if (activeTurnAmountRewardDatas[i].RewardEffect is ITurnRewardModifierEffect modifierEffect)
                {
                    activeModifiers[i] = modifierEffect.Perform(turnAmount, turnAmountRewardLevels[i]);
                }
            }
        }
        
        private void OnTurnSequenceCompleted()
        {
            for (int i = 0; i < activeTurnAmountRewardDatas.Length; i++)
            {
                if (activeTurnAmountRewardDatas[i].RewardEffect is ITurnRewardModifierEffect modifierEffect)
                {
                    modifierEffect.Revert(activeModifiers[i]);
                }
            }
        }

    }
}