using System.Diagnostics;

namespace TimelessEchoes.MapGeneration
{
    // Yield according to work performed, rather than imposing a frame delay on
    // every small batch. Restart after resuming so time spent waiting is excluded.
    internal struct GenerationWorkBudget
    {
        private long started;
        private readonly long allowance;

        public GenerationWorkBudget(bool initialLoad)
        {
            allowance = (long)(Stopwatch.Frequency * (initialLoad ? 0.006 : 0.002));
            started = Stopwatch.GetTimestamp();
        }

        public bool Expired => Stopwatch.GetTimestamp() - started >= allowance;
        public void Restart() => started = Stopwatch.GetTimestamp();
    }
}
