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
        /// Result of the most recent roll. 1 indicates the first face.
        /// </summary>
        public int Result { get; private set; } = 1;

        /// <summary>
        /// Stop any ongoing roll animation and hide the dice.
        /// </summary>
        public void ResetRoll()
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

        /// <summary>
        /// Perform a dice roll animation using the configured durations.
        /// </summary>
        public IEnumerator Roll()
        {
            yield return Roll(rollDuration + resultDuration);
        }

        /// <summary>
        /// Perform a dice roll that lasts the specified <paramref name="totalDuration"/> seconds.
        /// Durations are scaled based on the default roll and result times.
        /// </summary>
        public IEnumerator Roll(float totalDuration)
        {
            if (diceRenderer == null || faces == null || faces.Length == 0)
                yield break;

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

            Result = Random.Range(1, faces.Length + 1);
            diceRenderer.sprite = faces[Result - 1];
            yield return new WaitForSeconds(scaledResultDuration);
            diceRenderer.enabled = false;
        }
    }
}
