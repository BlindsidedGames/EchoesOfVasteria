#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using TimelessEchoes.Skills;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Buffs;
using TimelessEchoes.Hero;
using TimelessEchoes.Enemies;

namespace TimelessEchoes.Editor
{
    public class GameDataManager : OdinMenuEditorWindow
    {
        [MenuItem("Tools/Game Data Manager")]
        private static void Open()
        {
            GetWindow<GameDataManager>();
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree();
            tree.AddAllAssetsAtPath("Skills", "Assets", typeof(Skill), true);
            tree.AddAllAssetsAtPath("Stat Upgrades", "Assets", typeof(StatUpgrade), true);
            tree.AddAllAssetsAtPath("Resources", "Assets", typeof(Resource), true);
            tree.AddAllAssetsAtPath("Buff Recipes", "Assets", typeof(BuffRecipe), true);
            tree.AddAllAssetsAtPath("Hero Stats", "Assets", typeof(HeroStats), true);
            tree.AddAllAssetsAtPath("Enemy Stats", "Assets", typeof(EnemyData), true);

            tree.Config.DrawSearchToolbar = true;
            tree.Config.SearchToolbarHeight = 20;

            return tree;
        }
    }
}
#endif
