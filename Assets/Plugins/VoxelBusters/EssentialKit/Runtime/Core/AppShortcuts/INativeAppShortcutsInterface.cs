using System;
using System.Threading;
using System.Threading.Tasks;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.AppShortcutsCore
{
    public interface INativeAppShortcutsInterface : INativeFeatureInterface
    {
        #region Events

        event ShortcutClickedInternalCallback OnShortcutClicked;

        #endregion
        
        #region Methods

        void Add(AppShortcutItem item);
        void Remove(string shortcutItemId);
        
        #endregion
    }
}