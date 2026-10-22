#if UNITY_INCLUDE_TESTS
using System.Linq;
using NUnit.Framework;
using TimelessEchoes.Farming;
using TimelessEchoes.Tasks;
using UnityEngine;
namespace Tests.EditMode
{
    public sealed class FarmSeedChanceTests
    {
        [Test]
        public void LaterCropSeedsAreRarerAndOrchardsDoNotChangeTheirChances()
        {
            var content = ScriptableObject.CreateInstance<FarmContent>();
            var sources = Enumerable.Range(1, 17).Select(level => { var task = ScriptableObject.CreateInstance<TaskData>(); task.requiredSkillLevel = level; return task; }).ToArray();
            try
            {
                content.recipes = sources.Select((task, i) => new FarmRecipe { id = "crop-" + i, source = task }).ToList();
                Assert.AreEqual(.05f, content.SeedDropChance(content.recipes[0]), .000001f);
                var previous = .06f;
                foreach (var recipe in content.recipes)
                {
                    var chance = content.SeedDropChance(recipe);
                    Assert.Greater(chance, 0); Assert.Less(chance, previous);
                    previous = chance;
                }
                Assert.Less(previous, .01f, "The final crop should drop fewer than one pack per 100 eligible tasks on average.");
                var first = content.recipes[0]; var last = content.recipes[16];
                var orchard = new FarmRecipe { id = "earlier-tree", source = sources[0], orchard = true };
                content.recipes.Reverse(); content.recipes.Insert(0, orchard);
                Assert.AreEqual(.05f, content.SeedDropChance(first), .000001f);
                Assert.AreEqual(previous, content.SeedDropChance(last), .000001f);
                Assert.Zero(content.SeedDropChance(orchard));
                Assert.Zero(content.SeedDropChance(null));
            }
            finally { foreach (var source in sources) Object.DestroyImmediate(source); Object.DestroyImmediate(content); }
        }
    }
}
#endif
