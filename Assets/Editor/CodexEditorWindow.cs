using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window for managing kill codex data.
/// </summary>
public class CodexEditorWindow : OdinEditorWindow
{
    [MenuItem("Tools/Codex Editor")]
    private static void Open()
    {
        GetWindow<CodexEditorWindow>().Show();
    }

    [InlineEditor] public KillCodexDatabase database;
}
