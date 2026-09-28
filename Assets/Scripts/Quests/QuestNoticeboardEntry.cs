namespace TimelessEchoes.Quests
{
    public enum QuestNoticeboardCategory { Ready, Pinned, Active, Completed }

    public readonly struct QuestNoticeboardEntry
    {
        public readonly QuestData Quest;
        public readonly QuestNoticeboardCategory Category;
        public readonly float Progress;
        public bool Completed => Category == QuestNoticeboardCategory.Completed;
        public QuestNoticeboardEntry(QuestData quest, QuestNoticeboardCategory category, float progress)
        { Quest = quest; Category = category; Progress = progress; }
    }
}
