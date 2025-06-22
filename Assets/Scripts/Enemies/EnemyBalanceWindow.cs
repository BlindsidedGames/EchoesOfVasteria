#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace TimelessEchoes.Enemies
{
    public class EnemyBalanceWindow : OdinMenuEditorWindow
    {
        [MenuItem("Tools/Enemy Balance")]
        private static void Open()
        {
            GetWindow<EnemyBalanceWindow>();
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree();
            tree.AddAllAssetsAtPath("Enemies", "Assets", typeof(EnemyStats), true);
            return tree;
        }
    }
}
#endif
