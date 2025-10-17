using Sirenix.OdinInspector;
using UnityEngine;

namespace Variables
{
    [CreateAssetMenu(fileName = "New Sprite Variable", menuName = "Variable/Sprite", order = 0)]
    public class SpriteVariable : ScriptableObject
    {
        [SerializeField, AssetSelector]
        private Sprite sprite;
        
        public Sprite Value => sprite;

        public string ToTag()
        {
            return $"<sprite={0}>";
        }
    }
}