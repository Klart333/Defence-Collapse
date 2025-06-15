using TMPro;
using UnityEngine;

namespace UI
{
    public class PooledText : PooledMonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI text;
        
        public TextMeshProUGUI Text => text;
    }
}