#if UNITY_EDITOR
namespace TimelessEchoes.Platform
{
    // Procedural task placement must not consume animation/particle randomness
    // when replaying a screenshot recipe. Normal Editor play keeps Unity's RNG.
    public static class EditorCaptureRandom
    {
        private static System.Random capture = new(104729);
        public static void Reset(int seed) => capture = new System.Random(seed);
        public static float value => StartupDiagnostics.EditorScreenshotSession ? (float)capture.NextDouble() : UnityEngine.Random.value;
        public static int Range(int minimum, int maximum) => StartupDiagnostics.EditorScreenshotSession
            ? capture.Next(minimum, maximum) : UnityEngine.Random.Range(minimum, maximum);
        public static float Range(float minimum, float maximum) => StartupDiagnostics.EditorScreenshotSession
            ? minimum + (maximum - minimum) * (float)capture.NextDouble() : UnityEngine.Random.Range(minimum, maximum);
    }
}
#endif
