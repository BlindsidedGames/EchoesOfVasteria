#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using TimelessEchoes.Enemies;
using TimelessEchoes.Gear;
using TimelessEchoes.Gear.UI;
using TimelessEchoes.Skills;
using TimelessEchoes.Stats;
using TimelessEchoes.UI.Toolkit;
using TimelessEchoes.Upgrades;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Tests.PlayMode
{
    public sealed partial class FieldsProductionPlayModeTests
    {
        [UnityTest]
        public IEnumerator CombatCrystalProgressionControlsEnemyDropsForgeAndSkills()
        {
            var owner = new GameObject("Protected Combat crystal progression");
            GameObject view = null, enemyObject = null;
            EnemyData data = null;
            var random = Random.state;
            try
            {
                var resources = owner.AddComponent<ResourceManager>();
                owner.AddComponent<GameplayStatTracker>().enabled = false;
                var skills = owner.AddComponent<SkillController>();
                var combat = AssetDatabase.LoadAssetAtPath<Skill>("Assets/Scriptables/Skills/Combat.asset");
                typeof(SkillController).GetField("combatSkill", Private).SetValue(skills, combat);
                typeof(SkillController).GetField("skills", Private).SetValue(skills, AssetDatabase.LoadAssetAtPath<ToolkitSkillsDefinition>("Assets/UI/Toolkit/Skills.asset").skills.ToList());
                Blindsided.EventHandler.LoadData();
                CreateGuidancePathfinder(owner);
                oracle.saveData.General.HubTutorialStage = 3;
                view = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Skills.prefab"));
                var screen = view.GetComponent<ToolkitSkillsScreen>();
                VisualElement ui = null;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tasks/Enemies/Slime/Slime_Green.prefab");
                data = Object.Instantiate(prefab.GetComponent<Enemy>().Stats); data.experience = 0;
                var slime = Resources.Load<Resource>("Resource Items/Slime");
                var pipeline = ConversionPipelineFactory.CreateCrystalPipeline(slime);
                var tiers = new[] { "Eznorb", "Nori", "Dlog", "Erif", "Lirium", "Copium", "Idle", "Vastium" };
                var levels = new[] { 1, 7, 15, 22, 30, 38, 46, 56 };
                void Kill()
                {
                    enemyObject = Object.Instantiate(prefab, owner.transform);
                    var enemy = enemyObject.GetComponent<Enemy>();
                    typeof(Enemy).GetField("stats", Private).SetValue(enemy, data);
                    enemy.InitForSpawn(); enemy.Health.KillImmediately();
                    if (enemyObject) Object.DestroyImmediate(enemyObject);
                    enemyObject = null;
                }
                for (var i = 0; i < tiers.Length; i++)
                {
                    var core = Resources.Load<CoreSO>("Gear/Cores/" + tiers[i]);
                    var crystal = core.crystalResource;
                    // Isolate this authored drop so the assertion measures eligibility, not RNG luck.
                    var authored = prefab.GetComponent<Enemy>().Stats.resourceDrops.Single(d => d.resource == crystal);
                    data.resourceDrops = new() { authored }; data.additionalLootChances = new();
                    resources.Add(core.chunkResource, 4, trackStats: false, eligibleForTierRoll: false);
                    resources.Add(slime, 2, trackStats: false, eligibleForTierRoll: false);
                    if (levels[i] > 1)
                    {
                        skills.GetProgress(combat).Level = levels[i] - 1;
                        Assert.True(screen.Show()); ui = view.GetComponent<UIDocument>().rootVisualElement; yield return null;
                        FieldsClick(ui.Q<Button>("skill-Combat")); yield return null;
                        Assert.True(ui.Q("unlock-" + crystal.name).ClassListContains("locked"));
                        screen.Hide();
                        Kill();
                        Assert.Zero(resources.GetAmount(crystal), crystal.name + " must not drop below its Combat level.");
                        Assert.False(pipeline.PerformConversion(resources, core));
                        Assert.Zero(pipeline.GetCalculatedMaxAmount(resources, core));
                    }
                    skills.GetProgress(combat).Level = levels[i];
                    Assert.True(screen.Show()); ui = view.GetComponent<UIDocument>().rootVisualElement; yield return null;
                    FieldsClick(ui.Q<Button>("skill-Combat")); yield return null;
                    var row = ui.Q("unlock-" + crystal.name);
                    Assert.True(row.ClassListContains("unlocked"));
                    Assert.AreSame(crystal.icon, row.Q<Image>().sprite);
                    if (i == 0) Assert.That(ui.Query<VisualElement>().ToList().Count(e => e.name.StartsWith("unlock-")), Is.EqualTo(8));
                    screen.Hide();
                    Kill();
                    var dropped = resources.GetAmount(crystal); Assert.Greater(dropped, 0, crystal.name);
                    Assert.True(pipeline.PerformConversion(resources, core));
                    Assert.Greater(resources.GetAmount(crystal), dropped);
                }
                // The existing Mining task unlock rows must still work with resource rows supported.
                Assert.True(screen.Show()); ui = view.GetComponent<UIDocument>().rootVisualElement; yield return null;
                FieldsClick(ui.Q<Button>("skill-Mining")); yield return null;
                Assert.NotNull(ui.Q("unlock-Eznorb Ore"));
            }
            finally
            {
                if (enemyObject) Object.DestroyImmediate(enemyObject);
                if (view) Object.DestroyImmediate(view);
                if (data) Object.DestroyImmediate(data);
                Object.DestroyImmediate(owner); Random.state = random;
            }
        }
    }
}
#endif
