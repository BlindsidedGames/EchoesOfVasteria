using UnityEngine;
using TimelessEchoes.Skills;

namespace TimelessEchoes.Hero
{
    /// <summary>
    /// Utility for spawning Echo helpers.
    /// </summary>
    public static class EchoManager
    {
        public static HeroController SpawnEcho(Skill skill, float duration)
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

                var hp = echoHero.GetComponent<HeroHealth>();
                if (hp != null)
                {
                    hp.Immortal = true;
                    hp.Init((int)hp.MaxHealth);
                }

                echoHero.AllowAttacks = false;

                var echo = obj.AddComponent<EchoController>();
                echo.Init(skill, duration);
            }

            return echoHero;
        }
    }
}
