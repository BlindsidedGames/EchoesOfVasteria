using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using System.IO;

#if UNITY_EDITOR
[ExecuteInEditMode]
public class EditorScreenshot : MonoBehaviour
{
    [Title("Screenshot Settings")]
    public Camera targetCamera;
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

        RenderTexture rt = new RenderTexture(width, height, 24);
        targetCamera.targetTexture = rt;

        Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGBA32, false);
        targetCamera.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenShot.Apply();

        targetCamera.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(rt);

        string directory = Path.Combine(Application.dataPath, "Screenshots");
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        string filename = $"Screenshot_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
        string path = Path.Combine(directory, filename);

        File.WriteAllBytes(path, screenShot.EncodeToPNG());
        Debug.Log($"Screenshot saved to: {path}");

        AssetDatabase.Refresh();
    }
}
#endif