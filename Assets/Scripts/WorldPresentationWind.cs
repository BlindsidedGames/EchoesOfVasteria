using TimelessEchoes.Hero;
using TimelessEchoes.Quests;
using UnityEngine;

namespace TimelessEchoes
{
    /// <summary>Temporary Orin gusts, using the same native sprites as his portrait.</summary>
    public sealed class WorldPresentationWind : MonoBehaviour
    {
        private QuestSpeaker speaker;
        private readonly SpriteRenderer[] gusts = new SpriteRenderer[3];
        private readonly Vector2[] offsets = { new(-1.15f, .3f), new(.9f, .65f), new(.2f, -.5f) };
        private Transform hero, map;
        private SpriteRenderer heroSprite;
        private float startedAt;
        private bool visible;

        public void Setup(QuestSpeaker profile)
        {
            speaker = profile;
            var controller = HeroController.Instance;
            var gm = GameManager.Instance;
            if (!profile || !controller || !gm || !gm.CurrentMap) return;
            hero = controller.transform;
            map = gm.CurrentMap.transform;
            heroSprite = controller.GetComponentInChildren<SpriteRenderer>();
            startedAt = Time.unscaledTime;
            for (var i = 0; i < gusts.Length; i++)
            {
                var gust = new GameObject("Orin wind " + (i + 1));
                gust.transform.SetParent(map, false);
                gusts[i] = gust.AddComponent<SpriteRenderer>();
                // Native sprite PPU is authoritative; there is no screen-size fit.
                gusts[i].transform.localScale = Vector3.one;
                gusts[i].color = new Color(1, 1, 1, .75f);
                gusts[i].enabled = false;
            }
        }

        public void SetVisible(bool value)
        {
            visible = value;
            if (!value) foreach (var gust in gusts) if (gust) gust.enabled = false;
        }

        private void LateUpdate()
        {
            var gm = GameManager.Instance;
            if (!visible || !speaker || !hero || !map || !gm || !gm.CurrentMap || gm.CurrentMap.transform != map)
            {
                foreach (var gust in gusts) if (gust) gust.enabled = false;
                return;
            }
            var duration = speaker.frames != null && speaker.frames.Length > 0
                ? speaker.frames.Length / Mathf.Max(1, speaker.framesPerSecond) + speaker.loopPauseSeconds : 1;
            for (var i = 0; i < gusts.Length; i++)
            {
                var gust = gusts[i];
                if (!gust) continue;
                gust.sprite = speaker.PortraitAt(Time.unscaledTime - startedAt + i * duration / gusts.Length);
                gust.enabled = gust.sprite;
                gust.transform.position = hero.position + (Vector3)offsets[i];
                if (heroSprite)
                {
                    gust.sortingLayerID = heroSprite.sortingLayerID;
                    gust.sortingOrder = heroSprite.sortingOrder + 1;
                }
            }
        }

        private void OnDestroy()
        {
            foreach (var gust in gusts) if (gust) Destroy(gust.gameObject);
        }
    }
}
