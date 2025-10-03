using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Turns
{
    [InlineEditor, CreateAssetMenu(fileName = "Reward Data", menuName = "Reward/Turn Amount/Turn Amount Reward", order = 0)]
    public class TurnAmountRewardData : SerializedScriptableObject
    {
        [SerializeField]
        private ITurnRewardEffect rewardEffect;
        
        public ITurnRewardEffect RewardEffect => rewardEffect;
    }
}