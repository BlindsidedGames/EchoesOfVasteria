using UnityEngine;
using Blindsided;

namespace Blindsided.Utilities
{
    [AddComponentMenu("Utilities/Show If Demo")]
    public class ShowIfDemo : MonoBehaviour
    {
        [SerializeField] private GameObject target;
        [SerializeField] private bool disableComponentAfterApply = true;

        private void Reset()
        {
            if (target == null)
                target = gameObject;
        }

        private void Start()
        {
            if (!Application.isPlaying)
                return;

            var oracle = Oracle.oracle;
            if (oracle == null)
                oracle = FindAnyObjectByType<Oracle>();

            if (oracle != null && oracle.demo)
            {
                var go = target != null ? target : gameObject;
                if (go != null)
                    go.SetActive(true);
            }

            if (disableComponentAfterApply)
                enabled = false;
        }
    }
}


