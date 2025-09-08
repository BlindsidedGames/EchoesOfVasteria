using TMPro;
using UnityEngine;

namespace TimelessEchoes.UI
{
    /// <summary>
    /// UI row for a single leaderboard entry. Holds rank, name, and score text references.
    /// </summary>
    public class LeaderboardEntryUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text versionText;

        public TMP_Text RankText => rankText;
        public TMP_Text NameText => nameText;
        public TMP_Text ScoreText => scoreText;
        public TMP_Text VersionText => versionText;

        private void Awake()
        {
            // Ensure rich text is enabled for size tags on the name field.
            if (nameText != null)
            {
                nameText.richText = true;
            }
        }

        public void Set(string rank, string name, string score)
        {
            if (rankText != null) rankText.text = rank ?? string.Empty;
            if (nameText != null) nameText.text = FormatNameWithDiscriminator(name);
            if (scoreText != null) scoreText.text = score ?? string.Empty;
        }

        public void SetActive(bool on)
        {
            gameObject.SetActive(on);
        }

        private static string FormatNameWithDiscriminator(string rawName)
        {
            if (string.IsNullOrEmpty(rawName)) return string.Empty;

            // Look for a trailing # followed by exactly 4 digits (e.g., "User#1234").
            // Wrap the username in <b> and the discriminator (including '#') in <i>.
            var name = rawName;
            int hashIndex = name.LastIndexOf('#');
            if (hashIndex >= 0 && hashIndex + 5 == name.Length)
            {
                bool fourDigits = true;
                for (int i = 1; i <= 4; i++)
                {
                    if (hashIndex + i >= name.Length || !char.IsDigit(name[hashIndex + i]))
                    {
                        fourDigits = false;
                        break;
                    }
                }

                if (fourDigits)
                {
                    string baseName = name.Substring(0, hashIndex);
                    string discriminator = name.Substring(hashIndex); // includes '#'
                    return "<b>" + EscapeForTMP(baseName) + "</b>" + "<i><size=70%>" + EscapeForTMP(discriminator) + "</size></i>";
                }
            }

            // No discriminator found; just escape the whole name
            return EscapeForTMP(name);
        }

        private static string EscapeForTMP(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            // Escape characters that would be treated as TMP rich text
            return s.Replace("&", "&amp;")
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;");
        }
    }
}
