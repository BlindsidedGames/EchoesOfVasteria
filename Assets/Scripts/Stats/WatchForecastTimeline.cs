using System;
using System.Collections.Generic;
using System.Linq;
using TimelessEchoes.Farming;

namespace TimelessEchoes.Stats
{
    // Presentation wire format. Frames are [seconds, distance, effective cap,
    // estimated resource gain, reaps, phase, task gain, kill gain] since origin.
    // Phase: 0 moving, 1 restart pause, 2 stopped pending player input.
    public sealed class WatchForecastTimeline
    {
        public int schemaVersion = 1;
        public string mapId;
        public double generatedAt, horizonSeconds;
        public bool distanceReaping, truncated;
        public List<double[]> frames = new();
        public const int MaxFrames = 128;

        public static WatchForecastTimeline Create(BackgroundRunEstimator.Input input,
            BackgroundRunEstimator.Result result, double resourcesPerTask, double resourcesPerKill,
            FarmBackgroundProjection fields = null)
        {
            if (result.Status != "forecast" || result.Steps.Count == 0 || result.Seconds <= 0 ||
                !Finite(resourcesPerTask) || !Finite(resourcesPerKill))
                return CreateFieldsOnly(input, result, fields);
            var timeline = new WatchForecastTimeline { mapId = input.Run.Map, distanceReaping = input.DistanceReaping };
            var frameLimit = fields != null && fields.HorizonSeconds > result.Seconds ? MaxFrames - 1 : MaxFrames;
            timeline.frames.Add(new[] { 0d, (double)input.Run.Distance, result.Steps[0].Cap, 0, 0, 0, 0, 0 });
            double seconds = 0, resources = 0, reaps = 0, tasks = 0, kills = 0;
            foreach (var step in result.Steps)
            {
                var oldCount = timeline.frames.Count;
                var oldLast = timeline.frames[oldCount - 1];
                var last = oldLast;
                if (last[2] != step.Cap)
                    timeline.Add(new[] { seconds, last[1], step.Cap, resources, reaps, 0, tasks, kills }, false);
                seconds += step.Seconds;
                resources += step.Tasks * resourcesPerTask + step.Kills * resourcesPerKill;
                tasks += step.Tasks; kills += step.Kills;
                timeline.Add(new[] { seconds, step.Distance, step.Cap, resources, reaps, 0, tasks, kills }, true);
                if (step.EndsRun)
                {
                    if (step.Reaped) reaps++;
                    // A completed run no longer fills a distance ring during its restart pause.
                    timeline.Add(new[] { seconds, 0, step.RestartCap, resources, reaps,
                        step.Reaped && input.StopOnFirstReap ? 2d : 1d, tasks, kills }, false);
                    if (step.RestartSeconds > 0)
                    {
                        seconds += step.RestartSeconds;
                        timeline.Add(new[] { seconds, 0, step.RestartCap, resources, reaps, 1, tasks, kills }, false);
                    }
                    if (step.Restarted)
                        timeline.Add(new[] { seconds, input.StartingDistance, step.RestartCap, resources, reaps, 0, tasks, kills }, false);
                }
                if (timeline.frames.Count <= frameLimit) continue;
                timeline.frames.RemoveRange(oldCount, timeline.frames.Count - oldCount);
                timeline.frames[oldCount - 1] = oldLast;
                timeline.truncated = true;
                break;
            }
            timeline.horizonSeconds = timeline.frames.Last()[0];
            timeline.IncludeFields(fields);
            return timeline.horizonSeconds > 0 && timeline.frames.All(f => Array.TrueForAll(f, Finite)) ? timeline : null;
        }

        private static WatchForecastTimeline CreateFieldsOnly(BackgroundRunEstimator.Input input,
            BackgroundRunEstimator.Result result, FarmBackgroundProjection fields)
        {
            if (fields == null || fields.Total <= 0) return null;
            var timeline = new WatchForecastTimeline { mapId = input.Run.Map, distanceReaping = input.DistanceReaping };
            var cap = input.DistanceReaping ? result.BaseCap : 0;
            timeline.frames.Add(new[] { 0d, (double)input.Run.Distance, cap, 0, 0, 2, 0, 0 });
            timeline.IncludeFields(fields);
            return timeline;
        }

        private void IncludeFields(FarmBackgroundProjection fields)
        {
            if (fields == null) return;
            // Preserve a truncated adventure's last truthful position. A complete
            // stopped adventure can still have crops growing for the rest of its hour.
            if (!truncated && horizonSeconds < fields.HorizonSeconds)
            {
                var last = (double[])frames[^1].Clone();
                last[0] = fields.HorizonSeconds;
                last[5] = 2;
                frames.Add(last);
                horizonSeconds = fields.HorizonSeconds;
            }
            // A small fixed sampling budget represents crop batches without adding
            // a native frame for every harvest or losing adventure boundary groups.
            var source = frames.ToArray();
            var samples = Math.Min(60, MaxFrames - frames.Count);
            for (var i = 1; i <= samples; i++)
            {
                var seconds = horizonSeconds * i / (samples + 1);
                if (source.Any(frame => Math.Abs(frame[0] - seconds) < .000001)) continue;
                frames.Add(Sample(source, seconds));
            }
            frames = frames.OrderBy(frame => frame[0]).ToList();
            foreach (var frame in frames) frame[3] += fields.At(frame[0]);
        }

        private static double[] Sample(double[][] source, double seconds)
        {
            var upper = Array.FindIndex(source, frame => frame[0] > seconds);
            var lower = Math.Max(0, upper < 0 ? source.Length - 1 : upper - 1);
            var value = (double[])source[lower].Clone();
            value[0] = seconds;
            if (upper < 0) return value;
            var next = source[upper];
            var fraction = (seconds - source[lower][0]) / (next[0] - source[lower][0]);
            foreach (var index in new[] { 3, 6, 7 })
                value[index] += (next[index] - value[index]) * fraction;
            if (value[5] == 0 && next[5] == 0 && value[2] == next[2] && value[4] == next[4])
                value[1] += (next[1] - value[1]) * fraction;
            return value;
        }

        private void Add(double[] point, bool compactMovement)
        {
            // Movement speed and cap must be unchanged to extend a straight segment.
            // Resources interpolate within that segment between the estimator's work totals;
            // no segment bridges a run ending, restart, cap change or reap-count change.
            if (compactMovement && frames.Count >= 2)
            {
                var a = frames[frames.Count - 2]; var b = frames[frames.Count - 1];
                var dt = b[0] - a[0]; var nextDt = point[0] - b[0];
                if (a[5] == 0 && b[5] == 0 && point[5] == 0 && a[2] == b[2] && b[2] == point[2] &&
                    a[4] == b[4] && b[4] == point[4] && dt > 0 && nextDt > 0 &&
                    Math.Abs((b[1] - a[1]) / dt - (point[1] - b[1]) / nextDt) < .000001)
                { frames[frames.Count - 1] = point; return; }
            }
            frames.Add(point);
        }

        // Keep a whole time-boundary group when making room in the 32KiB snapshot.
        public WatchForecastTimeline LimitedTo(int count)
        {
            count = Math.Min(count, frames.Count);
            while (count > 1 && count < frames.Count && frames[count - 1][0] == frames[count][0]) count--;
            if (count < 2 || frames[count - 1][0] <= 0) return null;
            return new WatchForecastTimeline { mapId = mapId, generatedAt = generatedAt,
                horizonSeconds = frames[count - 1][0], distanceReaping = distanceReaping,
                truncated = true, frames = frames.Take(count).ToList() };
        }
        private static bool Finite(double value) => double.IsFinite(value) && value >= 0;
    }
}
