using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Blindsided.UGS
{
    public class UgsDemoPanel : MonoBehaviour
    {
        [Header("Assign in Inspector")]
        public TMP_InputField nameInput;
        public TMP_Text statusText;
        public Transform rowsParent;
        public GameObject rowPrefab; // 3 TMP_Text children: rank, name, score

        async void Start()
        {
            statusText.text = "Checking profile...";
            var currentName = await LocalProfile.GetMyDisplayNameAsync();
            if (!string.IsNullOrWhiteSpace(currentName))
                statusText.text = $"Hello, {currentName}";
            else
                statusText.text = "Choose a display name.";
        }

        public async void OnSetName()
        {
            var desired = nameInput.text.Trim();
            if (desired.Length < 3 || desired.Length > 24)
            {
                statusText.text = "Name must be 3-24 chars.";
                return;
            }

            var (ok, msg) = await UniqueNameService.SetUniqueAsync(desired);
            statusText.text = ok ? $"Name set: {msg}" : msg;
        }

        public async void OnSubmitTestScore()
        {
            var score = Random.Range(100, 10000);
            await LeaderboardClient.SubmitAsync(score);
            statusText.text = $"Submitted score {score:N0}";
        }

        public async void OnShowTop()
        {
            var page = await LeaderboardClient.GetTopAsync(20);
            var ids = page.Results.Select(r => r.PlayerId);
            var names = await NameDirectory.GetDisplayNamesAsync(ids);

            foreach (Transform c in rowsParent) Destroy(c.gameObject);

            foreach (var e in page.Results)
            {
                var go = Instantiate(rowPrefab, rowsParent);
                var texts = go.GetComponentsInChildren<TMP_Text>();
                var hasName = names.TryGetValue(e.PlayerId, out var disp) && !string.IsNullOrWhiteSpace(disp);
                texts[0].text = e.Rank.ToString();
                var display = !string.IsNullOrWhiteSpace(e.PlayerName)
                    ? e.PlayerName
                    : hasName ? disp : $"Player {e.PlayerId[..6]}";
                texts[1].text = display;
                texts[2].text = e.Score.ToString("N0");
            }

            statusText.text = $"Top {page.Results.Count} loaded.";
        }
    }
}
