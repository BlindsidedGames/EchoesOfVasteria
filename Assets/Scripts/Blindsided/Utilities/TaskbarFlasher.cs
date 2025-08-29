#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Collections;
using System.Runtime.InteropServices;
using Sirenix.OdinInspector;
using UnityEngine;

public class TaskbarFlasher : MonoBehaviour
{
    private IntPtr unityHwnd = IntPtr.Zero;

    [StructLayout(LayoutKind.Sequential)]
    private struct FLASHWINFO
    {
        public uint cbSize;
        public IntPtr hwnd;
        public uint dwFlags;
        public uint uCount;
        public uint dwTimeout;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetActiveWindow();

    private const uint FLASHW_ALL = 3;
    private const uint FLASHW_TIMERNOFG = 12;

    private void Start()
    {
        // Save HWND once at startup
        unityHwnd = GetActiveWindow();
    }

    [Button]
    public void FlashNow()
    {
        if (unityHwnd == IntPtr.Zero) return;

        var fw = new FLASHWINFO();
        fw.cbSize = (uint)Marshal.SizeOf(fw);
        fw.hwnd = unityHwnd;
        fw.dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG;
        fw.uCount = 0; // flash until user interacts
        fw.dwTimeout = 0;

        FlashWindowEx(ref fw);
    }

    public void FlashAfterDelay(float delaySeconds)
    {
        StartCoroutine(FlashCoroutine(delaySeconds));
    }

    private IEnumerator FlashCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        FlashNow();
    }

    [Button]
    public void StopFlashing()
    {
        if (unityHwnd == IntPtr.Zero) return;

        var fw = new FLASHWINFO();
        fw.cbSize = (uint)Marshal.SizeOf(fw);
        fw.hwnd = unityHwnd;
        fw.dwFlags = 0; // stop
        fw.uCount = 0;
        fw.dwTimeout = 0;

        FlashWindowEx(ref fw);
    }
}
#else
using UnityEngine;

public class TaskbarFlasher : MonoBehaviour
{
    public void FlashNow() { }
    public void FlashAfterDelay(float delaySeconds) { }
    public void StopFlashing() { }
}
#endif