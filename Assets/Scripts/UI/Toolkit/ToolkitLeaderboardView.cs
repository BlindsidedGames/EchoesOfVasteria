using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
namespace TimelessEchoes.UI.Toolkit
{
    /// <summary>Owns only its visual subtree; stale requests cannot update a closed or changed view.</summary>
    public sealed class ToolkitLeaderboardView : IDisposable
    {
        private readonly ToolkitStatisticsDefinition definition;
        private readonly ToolkitTheme theme;
        private readonly IToolkitLeaderboardSource source;
        private readonly ScrollView scroll;
        private readonly Label title, status;
        private readonly Button refresh;
        private readonly Image topIcon, tasksIcon;
        private bool top, tasks, disposed;
        private int requestVersion;
        public Task Pending { get; private set; } = Task.CompletedTask;
        public ToolkitLeaderboardView(VisualElement body, VisualElement footer, ToolkitTheme theme, ToolkitStatisticsDefinition definition, IToolkitLeaderboardSource source = null)
        {
            this.theme = theme; this.definition = definition; this.source = source ?? new ToolkitLeaderboardSource();
            
            body.style.paddingTop = body.style.paddingBottom = body.style.paddingLeft = body.style.paddingRight = 3;
            title = ToolkitControls.Text("Completion Time", ToolkitControls.TextRole.Heading); title.style.unityTextAlign = TextAnchor.MiddleCenter; body.Add(title);
            status = ToolkitControls.Text("", ToolkitControls.TextRole.Body); status.name = "rank-status"; status.style.unityTextAlign = TextAnchor.MiddleCenter; body.Add(status);
            scroll = ToolkitControls.RecessedScroll(body, "leaderboard", theme, definition.inset);
            topIcon = Toggle(footer, "rank-top", "Show Top", () => { top = !top; Refresh(); });
            tasksIcon = Toggle(footer, "rank-tasks", "Seasonal", () => { tasks = !tasks; Refresh(); });
            refresh = ToolkitControls.Button("rank-refresh", Refresh, definition.button); refresh.AddToClassList("eov-control--tab"); refresh.text = "Refresh"; footer.Add(refresh);
            Refresh();
        }
        private Image Toggle(VisualElement footer, string name, string text, Action action)
        {
            var button = ToolkitControls.Button(name, action, definition.button); button.AddToClassList("eov-control--tab"); button.AddToClassList("eov-rank-toggle");
            var label = ToolkitControls.Text(text, ToolkitControls.TextRole.Heading); button.Add(label);
            var icon = ToolkitControls.Icon(definition.toggleOff); icon.style.width = 24; icon.style.height = 12; button.Add(icon); footer.Add(button); return icon;
        }
        public void Refresh()
        {
            if (disposed) return;
            title.text = tasks ? "Total Tasks Completed" : "Completion Time";
            ToolkitGameplay.SetToggle(topIcon,top);ToolkitGameplay.SetToggle(tasksIcon,tasks);
            Pending = Load(++requestVersion, top, tasks);
        }
        private async Task Load(int version, bool topMode, bool taskMode)
        {
            refresh.SetEnabled(false); status.text = "Loading..."; scroll.Clear();
            try
            {
                var snapshot = await source.Load(topMode, taskMode);
                if (disposed || version != requestVersion) return;
                status.text = !topMode && snapshot.Player == null ? "You need to submit a score to see your rank" : "";
                VisualElement player = null;
                foreach (var item in LeaderboardPresentation.Rows(snapshot, topMode))
                {
                    var row = new VisualElement { name = "rank-" + item.Entry.Rank }; row.AddToClassList("eov-rank-row"); row.EnableInClassList("selected",item.IsPlayer); scroll.Add(row);
                    var rank = (item.Entry.Rank + 1).ToString("N0");
                    if (item.IsPlayer && !topMode) rank += " / " + (snapshot.Total?.ToString("N0") ?? "?");
                    AddText(row, "rank", rank);
                    var rawName = string.IsNullOrWhiteSpace(item.Entry.PlayerName) ? item.IsPlayer ? snapshot.PlayerName : "-" : item.Entry.PlayerName;
                    AddName(row, rawName);
                    AddText(row, "score", LeaderboardPresentation.Score(item.Entry.Score, taskMode));
                    AddText(row, "version", LeaderboardPresentation.Version(item.Entry.Metadata));
                    if (item.IsPlayer) player = row;
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
                scroll.Clear(); status.text = "Leaderboard unavailable. Try refreshing.";
            }
            finally { if (!disposed && version == requestVersion) refresh.SetEnabled(true); }
        }
        private static void AddName(VisualElement row, string value)
        {
            // User names are literal data. TextCore does not decode TMP's HTML-style entities.
            var container = new VisualElement { name = "name" }; container.AddToClassList("eov-rank-name"); row.Add(container);
            var index = value.LastIndexOf('#');
            bool discriminator = index >= 0 && index + 5 == value.Length;
            if (discriminator) for (var i = index + 1; i < value.Length; i++) discriminator &= char.IsDigit(value[i]);
            var label = ToolkitControls.Text(discriminator ? value.Substring(0, index) : value);
            label.enableRichText = false; label.style.whiteSpace = WhiteSpace.Normal; container.Add(label);
            if (discriminator)
            {
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                var suffix = ToolkitControls.Text(value.Substring(index), ToolkitControls.TextRole.Caption);
                suffix.enableRichText = false; suffix.style.unityFontStyleAndWeight = FontStyle.Italic; container.Add(suffix);
            }
        }
        private static void AddText(VisualElement row, string column, string value)
        {
            var label = ToolkitControls.Text(value, column == "version" ? ToolkitControls.TextRole.Caption : ToolkitControls.TextRole.Body);
            label.enableRichText = false; label.name = column; label.AddToClassList("eov-rank-" + column); row.Add(label);
        }
        public void Dispose() { disposed = true; requestVersion++; }
    }
}
