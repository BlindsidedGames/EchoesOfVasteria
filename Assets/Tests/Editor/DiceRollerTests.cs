using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TimelessEchoes.Tests
{
    public class DiceRollerTests
    {
        private GameObject obj;
        private DiceRoller dice;
        private SpriteRenderer renderer;

        [SetUp]
        public void SetUp()
        {
            obj = new GameObject();
            renderer = obj.AddComponent<SpriteRenderer>();
            dice = obj.AddComponent<DiceRoller>();

            var rendererField = typeof(DiceRoller).GetField("diceRenderer", BindingFlags.NonPublic | BindingFlags.Instance);
            rendererField.SetValue(dice, renderer);

            var facesField = typeof(DiceRoller).GetField("faces", BindingFlags.NonPublic | BindingFlags.Instance);
            var faces = new Sprite[6];
            for (int i = 0; i < faces.Length; i++)
                faces[i] = Sprite.Create(Texture2D.blackTexture, new Rect(0,0,1,1), Vector2.zero);
            facesField.SetValue(dice, faces);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(obj);
        }

        [Test]
        public void ResetRollDisablesRenderer()
        {
            renderer.enabled = true;
            dice.ResetRoll();
            Assert.IsFalse(renderer.enabled);
        }

        [UnityTest]
        public IEnumerator RollSetsResult()
        {
            yield return dice.Roll(0.1f);
            Assert.GreaterOrEqual(dice.Result, 1);
            Assert.LessOrEqual(dice.Result, 6);
        }
    }
}
