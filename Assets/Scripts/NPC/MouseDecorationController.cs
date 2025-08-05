using System.Collections;
using UnityEngine;

namespace TimelessEchoes.NPC
{
    public class MouseDecorationController : AnimalDecorationController
    {
        [SerializeField] private Vector2 eatInterval = new Vector2(5f, 10f);
        private Coroutine routine;

        protected override void OnEnable()
        {
            base.OnEnable();
            routine = StartCoroutine(EatRoutine());
        }

        protected void OnDisable()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
        }

        private IEnumerator EatRoutine()
        {
            while (true)
            {
                float wait = Random.Range(eatInterval.x, eatInterval.y);
                yield return new WaitForSeconds(wait);
                PauseMovement();
                if (Animator != null)
                {
                    Animator.Play("Eat");
                    yield return null;
                    yield return new WaitForSeconds(Animator.GetCurrentAnimatorStateInfo(0).length);
                }
                ResumeMovement();
            }
        }
    }
}

