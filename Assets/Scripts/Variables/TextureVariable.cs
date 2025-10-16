using UnityEngine;

namespace Variables
{
    [CreateAssetMenu(fileName = "New Texture Variable", menuName = "Variable/Texture", order = 0)]
    public class TextureVariable : ScriptableObject
    {
        [SerializeField]
        private Texture2D texture;
        
        public Texture2D Value => texture;
    }
}