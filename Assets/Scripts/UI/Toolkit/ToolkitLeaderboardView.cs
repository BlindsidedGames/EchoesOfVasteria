using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Services.Leaderboards.Models;
namespace TimelessEchoes.UI.Toolkit
{
    /// <summary>Read-only leaderboard view. Late requests cannot update a changed or closed view.</summary>
    public sealed class ToolkitLeaderboardView : IDisposable
    {
        private readonly IToolkitLeaderboardSource source;
        private readonly ScrollView scroll;
        private readonly Label status, scoreHeading;
        private readonly VisualElement playerFooter;
        private readonly Button completion, taskBoard, topPlayers, aroundMe, refresh;
        private bool top, tasks, disposed;
        private int requestVersion;
        public Task Pending { get; private set; } = Task.CompletedTask;
        public ToolkitLeaderboardView(VisualElement body, VisualElement footer, ToolkitTheme theme, ToolkitStatisticsDefinition definition, IToolkitLeaderboardSource source = null)
        {
            this.source = source ?? new ToolkitLeaderboardSource();
            body.style.paddingTop = body.style.paddingBottom = body.style.paddingLeft = body.style.paddingRight = 0;
            var controls = ToolkitGameplay.E(body, "rank-controls");
            var boards = ToolkitGameplay.E(controls, "rank-choice");
            completion = Choice(boards, "rank-completion", "Completion time", () => { tasks = false; Refresh(); });
            taskBoard = Choice(boards, "rank-tasks", "Tasks completed", () => { tasks = true; Refresh(); });
            var scope = ToolkitGameplay.E(controls, "rank-choice rank-scope");
            scope.AddToClassList("rank-choice"); scope.AddToClassList("rank-scope");
            topPlayers = Choice(scope, "rank-top", "Top players", () => { top = true; Refresh(); });
            aroundMe = Choice(scope, "rank-around", "Around me", () => { top = false; Refresh(); });
            refresh = Choice(controls, "rank-refresh", "Refresh", Refresh); refresh.AddToClassList("rank-refresh");
            status = ToolkitControls.Text(""); status.name = "rank-status"; status.AddToClassList("rank-status"); body.Add(status);
            var headings = ToolkitGameplay.E(body, "rank-headings");
            Cell(headings, "rank", "Rank"); Cell(headings, "name", "Player"); scoreHeading = Cell(headings, "score", "Time");
            scroll = ToolkitControls.RecessedScroll(body, "leaderboard", theme, definition.inset);
            playerFooter = ToolkitGameplay.E(footer, "rank-player-footer");
            Refresh();
        }
        private static Button Choice(VisualElement parent, string name, string label, Action action)
        {
            var b = ToolkitGameplay.B(parent, label, action); b.name = name; b.AddToClassList("rank-choice-button"); return b;
        }
        public void Refresh()
        {
            if (disposed) return;
            completion.EnableInClassList("active", !tasks); taskBoard.EnableInClassList("active", tasks);
            topPlayers.EnableInClassList("active", top); aroundMe.EnableInClassList("active", !top);
            scoreHeading.text = tasks ? "Tasks" : "Time";
            Pending = Load(++requestVersion, top, tasks);
        }
        private void Status(string text) { status.text = text; status.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex; }
        private async Task Load(int version, bool topMode, bool taskMode)
        {
            refresh.SetEnabled(false); Status("Loading…"); scroll.Clear(); playerFooter.Clear();
            try
            {
                var snapshot = await source.Load(topMode, taskMode);
                if (disposed || version != requestVersion) return;
                var rows = LeaderboardPresentation.Rows(snapshot, topMode);
                Status(rows.Count == 0 ? "No scores yet." : snapshot.Player == null ? "No submitted score yet. Showing top players." : "");
                VisualElement player = null;
                foreach (var item in rows)
                {
                    var row = Row(scroll.contentContainer, item.Entry, item.IsPlayer, snapshot.PlayerName, taskMode);
                    if (item.IsPlayer) player = row;
                }
                if (snapshot.Player != null)
                {
                    var pinned = Row(playerFooter, snapshot.Player, true, snapshot.PlayerName, taskMode);
                    if (snapshot.Total.HasValue) pinned.Q<Label>(className: "rank-you").text = "You · " + snapshot.Total.Value.ToString("N0") + " ranked";
                    if (snapshot.Total.HasValue) playerFooter.tooltip = "Your rank: " + (snapshot.Player.Rank + 1).ToString("N0") + " of " + snapshot.Total.Value.ToString("N0");
                }
                else
                {
                    var note = ToolkitControls.Text("Your score will appear here after submission."); note.AddToClassList("rank-footer-note"); playerFooter.Add(note);
                }
                if (player != null && !topMode)
                {
                    var target = player;
                    scroll.schedule.Execute(() => { if (!disposed && version == requestVersion && target.panel != null) scroll.ScrollTo(target); });
                }
                else scroll.scrollOffset = Vector2.zero;
            }
            catch (Exception)
            {
                if (disposed || version != requestVersion) return;
                scroll.Clear(); Status("Leaderboard unavailable. Try refreshing.");
            }
            finally { if (!disposed && version == requestVersion) refresh.SetEnabled(true); }
        }
        private static VisualElement Row(VisualElement parent, LeaderboardEntry entry, bool mine, string fallbackName, bool taskMode)
        {
            var row = ToolkitGameplay.B(parent, "", () => { }); row.name = "rank-" + entry.Rank;
            row.AddToClassList("eov-rank-row"); row.EnableInClassList("selected", mine);
            if (entry.Rank < 3) row.AddToClassList("rank-place-" + entry.Rank);
            Cell(row, "rank", (entry.Rank + 1).ToString("N0"));
            var name = ToolkitGameplay.E(row, "eov-rank-name");
            var raw = string.IsNullOrWhiteSpace(entry.PlayerName) ? mine ? fallbackName : "—" : entry.PlayerName;
            var split = raw.LastIndexOf('#'); var suffix = split >= 0 && split + 5 == raw.Length;
            if (suffix) for (var i = split + 1; i < raw.Length; i++) suffix &= char.IsDigit(raw[i]);
            var label = ToolkitControls.Text(suffix ? raw.Substring(0, split) : raw); label.enableRichText = false; label.AddToClassList("rank-player-name"); name.Add(label);
            if (suffix) { var tag = ToolkitControls.Text(raw.Substring(split)); tag.enableRichText = false; tag.AddToClassList("rank-discriminator"); name.Add(tag); }
            if (mine) { var you = ToolkitControls.Text("You"); you.AddToClassList("rank-you"); name.Add(you); }
            Cell(row, "score", LeaderboardPresentation.Score(entry.Score, taskMode));
            var metadata = LeaderboardPresentation.Version(entry.Metadata);
            row.tooltip = string.IsNullOrEmpty(metadata) ? raw : raw + "\n" + metadata;
            row.clicked += () =>
            {
                var next = parent.IndexOf(row) + 1;
                if (next < parent.childCount && parent[next].ClassListContains("rank-entry-detail")) { parent.RemoveAt(next); return; }
                var detail = ToolkitControls.Text(raw + (string.IsNullOrEmpty(metadata) ? "\nVersion information unavailable." : "\n" + metadata));
                detail.enableRichText = false; detail.AddToClassList("rank-entry-detail"); parent.Insert(next, detail);
            };
            return row;
        }
        private static Label Cell(VisualElement row, string column, string text)
        {
            var label = ToolkitControls.Text(text); label.enableRichText = false; label.AddToClassList("eov-rank-" + column); row.Add(label); return label;
        }
        public void Dispose() { disposed = true; requestVersion++; }
    }
}
