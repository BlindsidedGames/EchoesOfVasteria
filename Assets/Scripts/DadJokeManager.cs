using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes
{
    /// <summary>
    ///     Displays a random dad joke when the button is clicked.
    /// </summary>
    public class DadJokeManager : MonoBehaviour
    {
        [SerializeField] private Button jokeButton;
        [SerializeField] private GameObject textBox;
        [SerializeField] private TMP_Text jokeText;
        [SerializeField] private List<string> dadJokes = new();


        private void Start()
        {
            if (textBox != null)
                textBox.SetActive(false);
            if (jokeButton != null)
                jokeButton.onClick.AddListener(OnJokeButtonClicked);
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(1))
                if (textBox != null && textBox.activeSelf)
                    textBox.SetActive(false);
        }

        private void OnDestroy()
        {
            if (jokeButton != null)
                jokeButton.onClick.RemoveListener(OnJokeButtonClicked);
        }

        private void OnJokeButtonClicked()
        {
            ShowJoke();
        }

        private void ShowJoke()
        {
            if (dadJokes.Count > 0 && jokeText != null)
            {
                var index = Random.Range(0, dadJokes.Count);
                jokeText.text = dadJokes[index];
            }

            if (textBox != null)
                textBox.SetActive(true);
        }
    }
}