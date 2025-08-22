using UnityEngine;

namespace Variables
{
    [CreateAssetMenu(fileName = "New Color Variable", menuName = "Variable/Color", order = 0)]
    public class ColorVariable : ScriptableObject
    {
        [SerializeField]
        private Color color;
        
        public Color Value => color;

        public string ToTag()
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>";
        }
    }
}