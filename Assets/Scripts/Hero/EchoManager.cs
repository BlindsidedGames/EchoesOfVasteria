using UnityEngine;
using TimelessEchoes.Skills;
using System.Collections.Generic;

namespace TimelessEchoes.Hero
{
    /// <summary>
    /// Utility for spawning Echo helpers.
    /// </summary>
    public static class EchoManager
    {
        public static HeroController SpawnEcho(System.Collections.Generic.IEnumerable<Skill> skills, float duration, bool combat, bool disableSkills = false)
        {
            var hero = HeroController.Instance;
            if (hero == null)
                return null;

            HeroController.PrepareForEcho();
            GameObject obj = Object.Instantiate(hero.gameObject, hero.transform.position, hero.transform.rotation, hero.transform.parent);

            var echoHero = obj.GetComponent<HeroController>();
            if (echoHero != null)
            {
                foreach (var r in obj.GetComponentsInChildren<SpriteRenderer>())
                {
                    var c = r.color;
                    c.a = 0.7f;
                    r.color = c;
                }

                // Echoes share the primary hero's health. Keep the existing
                // HeroHealth component so required dependencies remain intact
                // but flag it as an echo so damage is redirected.
                var hp = echoHero.GetComponent<HeroHealth>();
                if (hp != null)
                    hp.Immortal = false; // ensure damage can be forwarded

                echoHero.AllowAttacks = combat;

                var echo = obj.AddComponent<EchoController>();
                echo.Init(skills, duration, disableSkills);
            }

            return echoHero;
        }

        public static HeroController SpawnEcho(Skill skill, float duration, bool combat, bool disableSkills = false)
        {
            return SpawnEcho(new System.Collections.Generic.List<Skill> { skill }, duration, combat, disableSkills);
        }
    }
}
