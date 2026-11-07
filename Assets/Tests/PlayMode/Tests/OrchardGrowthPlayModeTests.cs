#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TimelessEchoes.Hero;
using TimelessEchoes.Tasks;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public sealed partial class FieldsProductionPlayModeTests
    {
        [UnityTest]
        public IEnumerator OrchardTasksGrowBeforeHarvestAndResetWhenReused()
        {
            var actor = new GameObject("Orchard gardener");
            var tasks = new List<FruitHarvestTask>();
            var previousTimeScale = Time.timeScale;
            try
            {
                Time.timeScale = 1;
                var hero = actor.AddComponent<HeroController>(); hero.enabled = false;
                yield return null;
                foreach (var recipe in content.recipes.Where(r => r.id is "recipe.apple.v1" or "recipe.pear.v1" or "recipe.peach.v1" or "recipe.cherry.v1"))
                {
                    var task = Object.Instantiate(recipe.source.taskPrefab).GetComponent<FruitHarvestTask>();
                    tasks.Add(task); task.enabled = false;
                    var renderer = task.GetComponentsInChildren<SpriteRenderer>().Single(r => r.sprite == recipe.stages[0]);
                    var ground = renderer.transform.localPosition.y - renderer.sprite.pivot.y / renderer.sprite.pixelsPerUnit;
                    Assert.True(task.Claim(hero)); task.StartTask();
                    for (var stage = 1; stage < 4; stage++)
                    {
                        for (var tick = 0; task.ProgressRemaining > 1 - stage * .25f - .001f && tick < 10000; tick++)
                            task.Tick(hero);
                        Assert.AreSame(recipe.stages[stage], renderer.sprite, recipe.id + " growth stage " + stage);
                        Assert.AreEqual(ground, renderer.transform.localPosition.y - renderer.sprite.pivot.y / renderer.sprite.pixelsPerUnit, .001f);
                        Assert.False(task.IsComplete());
                    }
                    var completed = 0;
                    task.TaskCompleted += _ => completed++;
                    for (var tick = 0; !task.IsComplete() && tick < 10000; tick++) task.Tick(hero);
                    Assert.True(task.IsComplete()); Assert.True(task.IsExhausted);
                    Assert.True(renderer.enabled, "The harvested tree remains in the world.");
                    Assert.AreSame(recipe.stages[2], renderer.sprite);
                    task.Tick(hero); Assert.AreEqual(1, completed);
                    task.gameObject.SetActive(false); task.enabled = true; task.gameObject.SetActive(true); task.enabled = false;
                    Assert.False(task.IsComplete()); Assert.False(task.IsExhausted);
                    Assert.AreSame(recipe.stages[0], renderer.sprite, "A pooled tree starts growing again.");
                    Object.DestroyImmediate(task.gameObject); tasks.Remove(task);
                }
            }
            finally
            {
                foreach (var task in tasks) if (task) Object.DestroyImmediate(task.gameObject);
                Object.DestroyImmediate(actor); Time.timeScale = previousTimeScale;
            }
        }
    }
}
#endif
