using TMPro;
using UnityEngine;

namespace Variables
{
    [CreateAssetMenu(fileName = "New Font Variable", menuName = "Variable/Font", order = 0)]
    public class FontVariable : ScriptableObject
    {
        [SerializeField]
        private TMP_FontAsset font;
        
        public TMP_FontAsset Value => font;
    }
}