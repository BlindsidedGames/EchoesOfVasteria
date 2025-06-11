using UnityEngine;

namespace Blindsided
{
    public class CursorSetter : MonoBehaviour
    {
        public enum CursorOption
        {
            DownscaledTexture,
            ForceSoftware
        }

        [Tooltip("Cursor image to apply")] public Texture2D cursorTexture2D;
        [Tooltip("Choose how the cursor should be applied")]
        public CursorOption cursorOption = CursorOption.DownscaledTexture;
        [Tooltip("Scale factor for DownscaledTexture option")] [Range(0.1f, 1f)]
        public float downscaleFactor = 0.5f;

        private void Start()
        {
            ApplyCursor();
        }

        public void ApplyCursor()
        {
            if (cursorTexture2D == null)
            {
                Debug.LogWarning("Cursor texture not assigned in CursorSetter.");
                return;
            }

            CursorMode mode = CursorMode.Auto;
            Texture2D textureToUse = cursorTexture2D;

            if (cursorOption == CursorOption.DownscaledTexture)
            {
                int width = Mathf.RoundToInt(cursorTexture2D.width * downscaleFactor);
                int height = Mathf.RoundToInt(cursorTexture2D.height * downscaleFactor);
                textureToUse = ScaleTexture(cursorTexture2D, width, height);
            }
            else if (cursorOption == CursorOption.ForceSoftware)
            {
                mode = CursorMode.ForceSoftware;
            }

            Vector2 hotspot = new Vector2(textureToUse.width / 2f, textureToUse.height / 2f);
            Cursor.SetCursor(textureToUse, hotspot, mode);
        }

        private Texture2D ScaleTexture(Texture2D src, int width, int height)
        {
            RenderTexture rt = RenderTexture.GetTemporary(width, height);
            Graphics.Blit(src, rt);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D result = new Texture2D(width, height, src.format, false);
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            result.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            return result;
        }

    }
}
