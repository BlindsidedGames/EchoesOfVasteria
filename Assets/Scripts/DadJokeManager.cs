using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TimelessEchoes
{
    /// <summary>
    /// Displays a random dad joke when a target object is clicked.
    /// </summary>
    public class DadJokeManager : MonoBehaviour
    {
        [SerializeField] private GameObject clickTarget;
        [SerializeField] private GameObject textBox;
        [SerializeField] private TMP_Text jokeText;
        [SerializeField] private List<string> dadJokes = new();

        private Camera mainCamera;

        private void Awake()
        {
            mainCamera = Camera.main;
        }

        private void Start()
        {
            if (textBox != null)
                textBox.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    if (textBox != null && textBox.activeSelf)
                        textBox.SetActive(false);
                    return;
                }
                TryHandleClick();
            }

            if (Input.GetMouseButtonDown(1))
            {
                if (textBox != null && textBox.activeSelf)
                    textBox.SetActive(false);
            }
        }

        private void TryHandleClick()
        {
            if (clickTarget == null || mainCamera == null)
                return;

            var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            bool hit = false;
            if (Physics.Raycast(ray, out var hitInfo))
            {
                hit = hitInfo.collider != null && hitInfo.collider.gameObject == clickTarget;
            }
            else
            {
                var hit2D = Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity);
                hit = hit2D.collider != null && hit2D.collider.gameObject == clickTarget;
            }

            if (hit)
                ShowJoke();
        }

        private void ShowJoke()
        {
            if (dadJokes.Count > 0 && jokeText != null)
            {
                int index = Random.Range(0, dadJokes.Count);
                jokeText.text = dadJokes[index];
            }
            if (textBox != null)
                textBox.SetActive(true);
        }
    }
}
