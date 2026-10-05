using TimelessEchoes.UI.Toolkit;
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
        private string currentJokeEnglish;


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

        private void OnEnable() => ToolkitLocalization.Changed += RefreshJokeLocale;
        private void OnDisable() => ToolkitLocalization.Changed -= RefreshJokeLocale;
        private void RefreshJokeLocale()
        {
            if (jokeText != null && currentJokeEnglish != null)
                jokeText.text = ToolkitLocalization.Text(JokeKey(currentJokeEnglish), currentJokeEnglish);
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

        public string NextJoke()
        {
            if (dadJokes.Count == 0) return "";
            if (currentJokeIndex >= dadJokes.Count) { ShuffleJokes(); currentJokeIndex = 0; }
            var english = dadJokes[currentJokeIndex++];
            currentJokeEnglish = english;
            return ToolkitLocalization.Text(JokeKey(english), english);
        }

        private static string JokeKey(string english)
        {
            using var hash = System.Security.Cryptography.SHA256.Create();
            var bytes = hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(english));
            return "joke." + System.BitConverter.ToString(bytes, 0, 8).Replace("-", string.Empty).ToLowerInvariant();
        }

        private void ShowJoke()
        {
            if (jokeText != null) jokeText.text = NextJoke();
            if (textBox != null) textBox.SetActive(true);
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