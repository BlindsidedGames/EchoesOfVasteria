using System.Collections.Generic;
using System.Linq;
using TimelessEchoes.Skills;
using TimelessEchoes.Tasks;
using UnityEngine;

namespace TimelessEchoes.Hero
{
    /// <summary>
    ///     Utility for spawning Echo helpers.
    /// </summary>
    public static class EchoManager
    {
        public static HeroController SpawnEcho(IEnumerable<Skill> skills, float duration, bool combat,
            bool disableSkills = false)
        {
            var hero = HeroController.Instance;
            if (hero == null)
                return null;

            HeroController.PrepareForEcho();
            var obj = Object.Instantiate(hero.gameObject, hero.transform.position, hero.transform.rotation,
                hero.transform.parent);

            var echoHero = obj.GetComponent<HeroController>();
            if (echoHero != null)
            {
                foreach (var r in obj.GetComponentsInChildren<SpriteRenderer>())
                {
                    var c = r.color;
                    c.a = 0.7f;
                    var newColor = c;

                    // Combat only echoes should appear slightly red to
                    // differentiate them from regular echoes.
                    if (combat && disableSkills)
                    {
                        newColor = new Color(1f, 0.5f, 0.5f, c.a);
                    }
                    else if (!disableSkills && skills != null)
                    {
                        var list = skills.Where(s => s != null).ToList();
                        if (list.Count == 1)
                        {
                            switch (list[0].skillName)
                            {
                                case "Farming":
                                    newColor = new Color(1f, 1f, 0f, c.a); // Yellow
                                    break;
                                case "Woodcutting":
                                    newColor = new Color(0f, 1f, 0f, c.a); // Green
                                    break;
                                case "Fishing":
                                    newColor = new Color(0f, 0.6f, 1f, c.a); // Blue
                                    break;
                                case "Mining":
                                    newColor = new Color(1f, 0.65f, 0f, c.a); // Orange
                                    break;
                                case "Looting":
                                    newColor = new Color(0.6f, 0f, 0.8f, c.a); // Purple
                                    break;
                            }
                        }
                    }

                    r.color = newColor;
                }

                // Echoes share the primary hero's health. Keep the existing
                // HeroHealth component so required dependencies remain intact
                // but flag it as an echo so damage is redirected.
                var hp = echoHero.GetComponent<HeroHealth>();
                if (hp != null)
                    hp.Immortal = false; // ensure damage can be forwarded

                echoHero.AllowAttacks = combat;

                if (disableSkills)
                {
                    var tc = obj.GetComponent<TaskController>();
                    if (tc != null)
                        Object.Destroy(tc);
                }

                var echo = obj.AddComponent<EchoController>();
                echo.Init(skills, duration, disableSkills, combat);
            }

            return echoHero;
        }

        public static HeroController SpawnEcho(Skill skill, float duration, bool combat, bool disableSkills = false)
        {
            return SpawnEcho(new List<Skill> { skill }, duration, combat, disableSkills);
        }
    }
}