using Sirenix.OdinInspector;
using UnityEngine;
using Variables;

namespace Juice.Cursor
{
    [CreateAssetMenu(fileName = "CursorTextureData", menuName = "Cursor/Cursor Texture", order = 0)]
    public class CursorTextureData : ScriptableObject
    {
        [Title("Cursor")]
        [SerializeField]
        private TextureReference cursorTexture;
        
        [SerializeField]
        private Vector2 hotspot;

        public static void SetCursorToData(CursorTextureData cursorData)
        {
            UnityEngine.Cursor.SetCursor(cursorData.cursorTexture.Value, cursorData.hotspot, CursorMode.Auto);
        }

        public static void SetCursorDefault()
        {
            UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}