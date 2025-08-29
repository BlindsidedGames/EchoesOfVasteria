using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TimelessEchoes.UI;

public static class AddButtonClickSfxTool
{
    [MenuItem("Tools/Audio/Add Button Click SFX To Scene")]
    private static void AddToActiveScene()
    {
        var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        var buttons = new List<Button>();
        foreach (var root in rootObjects)
        {
            // include inactive
            buttons.AddRange(root.GetComponentsInChildren<Button>(true));
        }

        int added = 0;
        int skipped = 0;

        foreach (var btn in buttons)
        {
            if (btn == null) { skipped++; continue; }
            var go = btn.gameObject;
            if (EditorUtility.IsPersistent(go)) { skipped++; continue; }

            if (go.GetComponent<ButtonClickSfx>() == null)
            {
                Undo.AddComponent<ButtonClickSfx>(go);
                EditorUtility.SetDirty(go);
                added++;
            }
            else
            {
                skipped++;
            }
        }

        if (added > 0)
        {
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        Debug.Log($"Add Button Click SFX: added {added}, skipped {skipped}.");
    }
}


