using System.Collections.Generic;
using UnityEngine;

namespace TimelessEchoes.Hero
{
    /// <summary>
    /// Manages activation of Echoes based on distance to the main hero.
    /// Echoes far away from the hero are deactivated to conserve resources
    /// and reactivated when the hero approaches again.
    /// </summary>
    [RequireComponent(typeof(HeroController))]
    public class EchoActivator : MonoBehaviour
    {
        [SerializeField] private float activationDistance = 10f;

        private HeroController hero;

        private void Awake()
        {
            hero = GetComponent<HeroController>();
        }

        private void LateUpdate()
        {
            if (hero == null)
                return;

            var pos = hero.transform.position;

            for (int i = EchoController.AllEchoes.Count - 1; i >= 0; i--)
            {
                var echo = EchoController.AllEchoes[i];
                if (echo == null)
                {
                    EchoController.AllEchoes.RemoveAt(i);
                    continue;
                }

                var echoHero = echo.GetComponent<HeroController>();
                if (echoHero == null || echoHero == hero)
                    continue;

                var dist = Vector2.Distance(pos, echo.transform.position);
                echoHero.SetActiveState(dist <= activationDistance);
            }
        }
    }
}
