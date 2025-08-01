using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
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

        private int currentJokeIndex;


        private void Start()
        {
            if (dadJokes.Count > 0)
            {
                ShuffleJokes();
                currentJokeIndex = 0;
            }
            if (textBox != null)
                textBox.SetActive(false);
            if (jokeButton != null)
                jokeButton.onClick.AddListener(OnJokeButtonClicked);
        }

        private void Update()
        {
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                if (textBox != null && textBox.activeSelf)
                    textBox.SetActive(false);
            }
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
                if (currentJokeIndex >= dadJokes.Count)
                {
                    ShuffleJokes();
                    currentJokeIndex = 0;
                }

                jokeText.text = dadJokes[currentJokeIndex];
                currentJokeIndex++;
            }

            if (textBox != null)
                textBox.SetActive(true);
        }

        private void ShuffleJokes()
        {
            for (var i = dadJokes.Count - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                (dadJokes[i], dadJokes[j]) = (dadJokes[j], dadJokes[i]);
            }
        }
    }
}