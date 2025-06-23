using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Upgrades
{
    [InlineEditor, CreateAssetMenu(fileName = "Rank Color Utility", menuName = "Upgrade/Rank Color Utility", order = 0)]
    public class RankColorUtility : ScriptableObject
    {
        [SerializeField]
        private Color[] rankColors;
        
        public Color GetColor(int rank) => rankColors[rank];
        public Color GetColor(UpgradeRank rank) => rankColors[(int)rank];
    }
}