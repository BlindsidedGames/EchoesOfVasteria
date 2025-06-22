using System.Collections;
using UnityEngine;

namespace Blindsided.Utilities.Pooling
{
    /// <summary>
    /// Automatically returns an object to its pool after a delay.
    /// </summary>
    public class PoolAutoReturn : MonoBehaviour
    {
        public void ReturnAfter(float delay)
        {
            StartCoroutine(ReturnRoutine(delay));
        }

        private IEnumerator ReturnRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            PoolManager.Release(gameObject);
        }
    }
}
