using UnityEngine.Localization;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Variables
{
    [InlineEditor, CreateAssetMenu(fileName = "New String Variable", menuName = "Variable/String", order = 0)]
    public class StringVariable : ScriptableObject
    {
        [SerializeField]
        private LocalizedString text;
        
        public LocalizedString LocalizedText => text;
        public string Value => text.GetLocalizedString();
    }
}