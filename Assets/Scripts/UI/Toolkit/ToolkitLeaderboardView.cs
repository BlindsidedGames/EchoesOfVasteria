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
        private LeaderboardSnapshot cachedSnapshot;
        private bool cachedTop, cachedTasks;
        private bool failed;
        public Task Pending { get; private set; } = Task.CompletedTask;
        public ToolkitLeaderboardView(VisualElement body, VisualElement footer, ToolkitTheme theme, ToolkitStatisticsDefinition definition, IToolkitLeaderboardSource source = null)
        {
            this.source = source ?? new ToolkitLeaderboardSource();
            body.style.paddingTop = body.style.paddingBottom = body.style.paddingLeft = body.style.paddingRight = 0;
            var controls = ToolkitGameplay.E(body, "rank-controls");
            var boards = ToolkitGameplay.E(controls, "rank-choice");
            completion = LocalizedChoice(boards, "rank-completion", "leaderboard.completion-time", "Completion time", () => { tasks = false; Refresh(); });
            taskBoard = LocalizedChoice(boards, "rank-tasks", "leaderboard.tasks-completed", "Tasks completed", () => { tasks = true; Refresh(); });
            var scope = ToolkitGameplay.E(controls, "rank-choice rank-scope");
            scope.AddToClassList("rank-choice"); scope.AddToClassList("rank-scope");
            topPlayers = LocalizedChoice(scope, "rank-top", "leaderboard.top-players", "Top players", () => { top = true; Refresh(); });
            aroundMe = LocalizedChoice(scope, "rank-around", "leaderboard.around-me", "Around me", () => { top = false; Refresh(); });
            refresh = LocalizedChoice(controls, "rank-refresh", "common.refresh", "Refresh", Refresh); refresh.AddToClassList("rank-refresh");
            status = ToolkitControls.Text(""); status.name = "rank-status"; status.AddToClassList("rank-status"); body.Add(status);
            var headings = ToolkitGameplay.E(body, "rank-headings");
            LocalizedCell(headings, "rank", "leaderboard.rank", "Rank"); LocalizedCell(headings, "name", "leaderboard.player", "Player"); scoreHeading = Cell(headings, "score", ToolkitLocalization.Text("leaderboard.time", "Time"));
            scroll = ToolkitControls.RecessedScroll(body, "leaderboard", theme, definition.inset);
            playerFooter = ToolkitGameplay.E(footer, "rank-player-footer");
            Refresh();
        }
        private static Button LocalizedChoice(VisualElement parent, string name, string key, string english, Action action)
        { var button = Choice(parent, name, "", action); ToolkitLocalization.Bind(button, key, english); return button; }
        private static Label LocalizedCell(VisualElement row, string column, string key, string english)
        { var label = Cell(row, column, ""); ToolkitLocalization.Bind(label, key, english); return label; }
        private static Button Choice(VisualElement parent, string name, string label, Action action)
        {
            var b = ToolkitGameplay.B(parent, label, action); b.name = name; b.AddToClassList("rank-choice-button"); return b;
        }
        public void Refresh()
        {
            if (disposed) return;
            completion.EnableInClassList("active", !tasks); taskBoard.EnableInClassList("active", tasks);
            topPlayers.EnableInClassList("active", top); aroundMe.EnableInClassList("active", !top);
            scoreHeading.text = tasks ? ToolkitLocalization.Text("leaderboard.tasks", "Tasks") : ToolkitLocalization.Text("leaderboard.time", "Time");
            Pending = Load(++requestVersion, top, tasks);
        }
        private void Status(string text) { status.text = text; status.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex; }
        private async Task Load(int version, bool topMode, bool taskMode)
        {
            cachedSnapshot = null; failed = false; refresh.SetEnabled(false); Status(ToolkitLocalization.Text("common.loading", "Loading…")); scroll.Clear(); playerFooter.Clear();
            try
            {
                var snapshot = await source.Load(topMode, taskMode);
                if (disposed || version != requestVersion) return;
                cachedSnapshot = snapshot; cachedTop = topMode; cachedTasks = taskMode; failed = false;
                Render(snapshot, topMode, taskMode, version);
            }
            catch (Exception)
            {
                if (disposed || version != requestVersion) return;
                failed = true; scroll.Clear(); Status(ToolkitLocalization.Text("leaderboard.unavailable", "Leaderboard unavailable. Try refreshing."));
            }
            finally { if (!disposed && version == requestVersion) refresh.SetEnabled(true); }
        }
        public void Relocalize()
        {
            if (disposed) return;
            scoreHeading.text = tasks ? ToolkitLocalization.Text("leaderboard.tasks", "Tasks") : ToolkitLocalization.Text("leaderboard.time", "Time");
            if (cachedSnapshot != null) Render(cachedSnapshot, cachedTop, cachedTasks, requestVersion);
            else Status(failed ? ToolkitLocalization.Text("leaderboard.unavailable", "Leaderboard unavailable. Try refreshing.") : ToolkitLocalization.Text("common.loading", "Loading…"));
        }
        private void Render(LeaderboardSnapshot snapshot, bool topMode, bool taskMode, int version)
        {
            scroll.Clear(); playerFooter.Clear();
                var rows = LeaderboardPresentation.Rows(snapshot, topMode);
                Status(rows.Count == 0 ? ToolkitLocalization.Text("leaderboard.no-scores", "No scores yet.") : snapshot.Player == null ? ToolkitLocalization.Text("leaderboard.no-submitted-score", "No submitted score yet. Showing top players.") : "");
                VisualElement player = null;
                foreach (var item in rows)
                {
                    var row = Row(scroll.contentContainer, item.Entry, item.IsPlayer, snapshot.PlayerName, taskMode);
                    if (item.IsPlayer) player = row;
                }
                if (snapshot.Player != null)
                {
                    var pinned = Row(playerFooter, snapshot.Player, true, snapshot.PlayerName, taskMode);
                    if (snapshot.Total.HasValue) pinned.Q<Label>(className: "rank-you").text = ToolkitLocalization.Text("leaderboard.total-ranked", "You · {0:N0} ranked", snapshot.Total.Value);
                    if (snapshot.Total.HasValue) playerFooter.tooltip = ToolkitLocalization.Text("leaderboard.your-rank", "Your rank: {0:N0} of {1:N0}", snapshot.Player.Rank + 1, snapshot.Total.Value);
                }
                else
                {
                    var note = ToolkitControls.Text(ToolkitLocalization.Text("leaderboard.submission-hint", "Your score will appear here after submission.")); note.AddToClassList("rank-footer-note"); playerFooter.Add(note);
                }
                if (player != null && !topMode)
                {
                    var target = player;
                    scroll.schedule.Execute(() => { if (!disposed && version == requestVersion && target.panel != null) scroll.ScrollTo(target); });
                }
                else scroll.scrollOffset = Vector2.zero;
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
            if (mine) { var you = ToolkitControls.Text(ToolkitLocalization.Text("leaderboard.you", "You")); you.AddToClassList("rank-you"); name.Add(you); }
            Cell(row, "score", LeaderboardPresentation.Score(entry.Score, taskMode));
            var metadata = LeaderboardPresentation.Version(entry.Metadata);
            row.tooltip = string.IsNullOrEmpty(metadata) ? raw : raw + "\n" + metadata;
            row.clicked += () =>
            {
                var next = parent.IndexOf(row) + 1;
                if (next < parent.childCount && parent[next].ClassListContains("rank-entry-detail")) { parent.RemoveAt(next); return; }
                var detail = ToolkitControls.Text(raw + (string.IsNullOrEmpty(metadata) ? "\n" + ToolkitLocalization.Text("leaderboard.no-version", "Version information unavailable.") : "\n" + metadata));
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
