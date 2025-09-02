#if UNITY_IOS || UNITY_TVOS
using System;

namespace VoxelBusters.EssentialKit.AppShortcutsCore.iOS
{
    internal delegate void OnShortcutClickedNativeCallback(string shortcutId);
}
#endif