#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TimelessEchoes.Editor.Capture
{
    [Serializable]
    public sealed class ScreenshotScenario
    {
        public int version = 1;
        public string name = "Adventure";
        public string mapGuid;
        public string window = "None";
        public int seed = 104729;
        public int runFrames = 120;
        public bool lightMode;
        public string progressSha256;
        public List<ShotPose> poses = new();
        public List<ShotScroll> scrolls = new();
        public List<ShotSelection> selections = new();
        public string capturedUnityVersion;

        public static ScreenshotScenario Read(string folder)
        {
            var scenario = JsonUtility.FromJson<ScreenshotScenario>(File.ReadAllText(Path.Combine(folder, "scenario.json")));
            if (scenario == null || scenario.version != 1 || string.IsNullOrWhiteSpace(scenario.name))
                throw new InvalidDataException("Unsupported or incomplete screenshot scenario.");
            if (scenario.runFrames < 0 || scenario.runFrames > 36000)
                throw new InvalidDataException("Scenario warm-up must be between 0 and 36,000 frames.");
            return scenario;
        }

        public void Write(string folder)
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "scenario.json"), JsonUtility.ToJson(this, true));
        }
    }

    [Serializable]
    public sealed class ShotPose
    {
        public string path;
        public Vector3 position, scale;
        public Quaternion rotation;
        public string spriteId;
        public bool hasSprite;
        public bool flipX, flipY;
        public Color color;
    }

    [Serializable]
    public sealed class ShotScroll
    {
        public string document, path;
        public Vector2 offset;
    }
    [Serializable]
    public sealed class ShotSelection
    {
        public string document, path, name;
    }

    public readonly struct ScreenshotSize
    {
        public readonly string id, label;
        public readonly int width, height;
        public bool Portrait => height >= width * 1.25f;
        public ScreenshotSize(string id, string label, int width, int height)
        { this.id = id; this.label = label; this.width = width; this.height = height; }
    }

    public static class ScreenshotSizes
    {
        // Checked against Apple, Google Play and Steam documentation on 2026-10-23.
        // These are capture targets, not a claim that every store accepts every size.
        public static readonly ScreenshotSize[] All =
        {
            new("steam", "Steam landscape — 2560 × 1440", 2560, 1440),
            new("google-phone", "Google phone — 1080 × 1920", 1080, 1920),
            new("google-tablet", "Google tablet 7/10-inch — 1920 × 1080", 1920, 1080),
            new("iphone-medium", "iPhone 6.3-inch — 1206 × 2622", 1206, 2622),
            new("iphone-large", "iPhone 6.9-inch — 1320 × 2868", 1320, 2868),
            new("ipad-landscape", "iPad 13-inch landscape — 2752 × 2064", 2752, 2064)
        };
    }
}
#endif
