using UnityEngine;

namespace Blindsided
{
    public class CursorSetter : MonoBehaviour
    {
        public Texture2D cursorTexture2D;

        void Start()
        {
            Vector2 hotspot = new Vector2(cursorTexture2D.width / 2f, cursorTexture2D.height / 2f);
            Cursor.SetCursor(cursorTexture2D, hotspot, CursorMode.Auto); 
        }

    }
}
