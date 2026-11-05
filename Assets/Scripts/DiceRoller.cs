using System.Collections;
using UnityEngine;

namespace TimelessEchoes
{
    /// <summary>
    /// Displays a dice roll using a SpriteRenderer.
    /// Cycles through the faces for a short time then shows the final face.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class DiceRoller : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer diceRenderer;
        [SerializeField] private Sprite[] faces = new Sprite[6];
        [SerializeField] private float rollDuration = 0.5f;
        [SerializeField] private float faceInterval = 0.1f;
        [SerializeField] private float resultDuration = 0.5f;

        /// <summary>
        /// Stop any ongoing roll animation and hide the dice.
        /// </summary>
        private void ResetRoll()
        {
            StopAllCoroutines();
            if (diceRenderer != null)
                diceRenderer.enabled = false;
        }

        private void Awake()
        {
            if (diceRenderer == null)
                diceRenderer = GetComponent<SpriteRenderer>();
            if (diceRenderer != null)
                diceRenderer.enabled = false;
        }

        private void OnDisable() => ResetRoll();

        /// <summary>
        /// Choose the result immediately and animate the dice independently of combat.
        /// </summary>
        public int Roll(float totalDuration)
        {
            ResetRoll();
            if (diceRenderer == null || faces == null || faces.Length == 0 || !isActiveAndEnabled)
                return 1;

            var result = Random.Range(1, faces.Length + 1);
            StartCoroutine(AnimateRoll(result, totalDuration));
            return result;
        }

        private IEnumerator AnimateRoll(int result, float totalDuration)
        {
            float baseDuration = rollDuration + resultDuration;
            float scale = baseDuration > 0f ? totalDuration / baseDuration : 1f;
            float scaledRollDuration = rollDuration * scale;
            float scaledFaceInterval = faceInterval * scale;
            float scaledResultDuration = resultDuration * scale;

            diceRenderer.enabled = true;
            float elapsed = 0f;
            while (elapsed < scaledRollDuration)
            {
                diceRenderer.sprite = faces[Random.Range(0, faces.Length)];
                elapsed += scaledFaceInterval;
                yield return new WaitForSeconds(scaledFaceInterval);
            }

            diceRenderer.sprite = faces[result - 1];
            yield return new WaitForSeconds(scaledResultDuration);
            diceRenderer.enabled = false;
        }
    }
}
