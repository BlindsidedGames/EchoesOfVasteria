using System;
using System.IO;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

namespace TimelessEchoes
{
    [ExecuteInEditMode]
    public class EditorScreenshot : MonoBehaviour
    {
        [Title("Screenshot Settings")] public Camera targetCamera;

        public int width = 1920;
        public int height = 1080;

        [Button("Take Screenshot")]
        private void TakeScreenshot()
        {
            if (targetCamera == null)
            {
                Debug.LogError("No camera assigned for screenshot.");
                return;
            }

            var rt = new RenderTexture(width, height, 24);

            // Save current camera settings so they can be restored afterwards.
            var prevTarget = targetCamera.targetTexture;
            var prevPixelRect = targetCamera.pixelRect;

            targetCamera.targetTexture = rt;
            targetCamera.pixelRect = new Rect(0f, 0f, width, height);

            var screenShot = new Texture2D(width, height, TextureFormat.RGBA32, false);
            targetCamera.Render();
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenShot.Apply();

            targetCamera.targetTexture = prevTarget;
            targetCamera.pixelRect = prevPixelRect;
            RenderTexture.active = null;
            DestroyImmediate(rt);

            var directory = Path.Combine(Application.dataPath, "Screenshots");
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var filename = $"Screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
            var path = Path.Combine(directory, filename);

            File.WriteAllBytes(path, screenShot.EncodeToPNG());
            Debug.Log($"Screenshot saved to: {path}");

            AssetDatabase.Refresh();
        }
    }
}

#endif