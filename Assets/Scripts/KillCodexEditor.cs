#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;

public class KillCodexEditor : OdinMenuEditorWindow
{
    [MenuItem("Codex/Manage Codex Data")]
    private static void Open()
    {
        GetWindow<KillCodexEditor>().Show();
    }

    protected override OdinMenuTree BuildMenuTree()
    {
        var tree = new OdinMenuTree();
        tree.AddAllAssetsAtPath("Codex Data", "Assets", typeof(GlobalCodexBuffData), true, true);
        return tree;
    }
}
#endif
