using System;
using System.Collections.Generic;
using System.Linq;
using Blindsided.SaveData;
using TimelessEchoes.MapGeneration;

namespace TimelessEchoes.Stats
{
    [Serializable]
    public sealed class BackgroundRunSample
    {
        public double Seconds, Distance, EndDistance, Tasks, Kills, DamageTaken;
        public double EchoTasks, EchoKills;
        public bool Died, Reaped;

        public static BackgroundRunSample From(GameData.RunRecord run) => new()
        {
            Seconds = run.ActiveSeconds > 0 ? run.ActiveSeconds : run.Duration,
            Distance = Math.Max(0, run.Distance - run.StartingDistance), EndDistance = run.Distance,
            Tasks = run.TasksCompleted, Kills = run.EnemiesKilled, DamageTaken = run.DamageTakenAsDouble,
            EchoTasks = run.EchoTasks, EchoKills = run.EchoKills, Died = run.Died, Reaped = run.Reaped
        };

        public static void Remember(GameData data, GameData.RunRecord run)
        {
            if (run.Estimated || run.Abandoned || run.Duration < 5 || run.Distance <= 0) return;
            data.BackgroundSamples ??= new();
            if (!data.BackgroundSamples.TryGetValue(run.MapType, out var samples))
                data.BackgroundSamples[run.MapType] = samples = new();
            samples.Add(From(run));
            if (samples.Count > 10) samples.RemoveRange(0, samples.Count - 10);
        }
    }

    // No scene, save, clock, RNG or inventory ownership. The caller supplies immutable
    // player/content observations and applies the returned work through normal owners.
    public static class BackgroundRunEstimator
    {
        public sealed class Input
        {
            public RunSaveData Run;
            public IReadOnlyList<BackgroundRunSample> Samples;
            public double Seconds, StartingDistance, BaseCap, CapMultiplier = 1, CapFlat;
            public double MaxHealth = 1, Regen, RestartSeconds = 20;
            public double GateDistance = double.PositiveInfinity;
            public bool DistanceReaping = true, CapGain = true, StopOnFirstReap;
            public Func<double, int, double> Pressure;
            public Func<double, double, (double multiplier, double flat)> CapModifiers;
            public double NextBuffExpiry = double.PositiveInfinity;
        }

        public sealed class Step
        {
            public double Seconds, Distance, DamageTaken, RestartSeconds, CapIncrease;
            public int Tasks, Kills;
            public bool EndsRun, Reaped;
        }

        public sealed class Result
        {
            public readonly List<Step> Steps = new();
            public int SampleCount, Reaps, Deaths, Tasks, Kills;
            public double Distance, BaseCap, HealthFraction, Seconds, MovingSeconds;
            public double TaskRemainder, KillRemainder, EchoTaskShare, EchoKillShare;
            public string Status = "forecast";
            public string StopReason;
        }

        public static Result Forecast(Input input)
        {
            var run = input.Run;
            if (run == null || !run.IsValid) throw new ArgumentException("A valid active run is required.");
            var result = new Result { Distance = run.Distance, BaseCap = input.BaseCap,
                HealthFraction = run.HealthFraction, TaskRemainder = run.Stats.TaskRemainder,
                KillRemainder = run.Stats.KillRemainder };
            var samples = (input.Samples ?? Array.Empty<BackgroundRunSample>())
                .Where(s => s != null && Finite(s.Seconds) && s.Seconds >= 5 &&
                    Finite(s.Distance) && s.Distance > 0 && Finite(s.Tasks) && Finite(s.Kills) &&
                    Finite(s.DamageTaken) && Finite(s.EndDistance)).TakeLast(10).ToArray();
            result.SampleCount = samples.Length;
            if (samples.Length == 0) { result.Status = "insufficientHistory"; return result; }
            if (!Finite(input.Seconds) || input.Seconds <= 0) return result;
            if (run.ReturnQueued || run.Dead || input.GateDistance <= run.Distance)
            { result.StopReason = "Player input required"; return result; }

            double seconds = 0, distance = 0, tasks = 0, kills = 0, damage = 0, reference = 0;
            double echoTasks = 0, echoKills = 0, weightSum = 0;
            for (var i = 0; i < samples.Length; i++)
            {
                var s = samples[i]; var weight = i + 1;
                seconds += weight * s.Seconds; distance += weight * s.Distance;
                tasks += weight * s.Tasks; kills += weight * s.Kills; damage += weight * s.DamageTaken;
                reference += weight * s.EndDistance * .5; weightSum += weight;
                echoTasks += weight * s.EchoTasks; echoKills += weight * s.EchoKills;
            }
            var speed = distance / seconds;
            if (!(speed > 0)) { result.Status = "insufficientHistory"; return result; }
            var taskRate = tasks / seconds; var killRate = kills / seconds;
            result.EchoTaskShare = tasks > 0 ? Math.Clamp(echoTasks / tasks, 0, 1) : 0;
            result.EchoKillShare = kills > 0 ? Math.Clamp(echoKills / kills, 0, 1) : 0;
            var referencePressure = Math.Max(.0001, input.Pressure?.Invoke(reference / weightSum, (int)(kills / weightSum * .5)) ?? 1);
            var observedDistance = samples.Max(s => s.EndDistance);
            var observedPressure = Math.Max(referencePressure, input.Pressure?.Invoke(observedDistance, (int)samples.Max(s => s.Kills)) ?? referencePressure);
            var health = input.MaxHealth * run.HealthFraction;
            var runKills = run.Stats.Kills;
            // Deliberately bounded prototype, not a new production offline policy.
            var remaining = Math.Min(input.Seconds, 3600);
            if (input.Seconds > remaining) result.StopReason = "Prototype limited to one hour";
            if ((taskRate + killRate) * remaining > 20000)
            { remaining = 20000 / (taskRate + killRate); result.StopReason = "Prototype work limit"; }
            while (remaining > .00001 && result.Steps.Count < 10000)
            {
                var modifiers = result.Reaps + result.Deaths == 0
                    ? input.CapModifiers?.Invoke(result.MovingSeconds, result.Distance) ?? (input.CapMultiplier, input.CapFlat)
                    : (1d, 0d);
                var cap = input.DistanceReaping ? RunDistanceRules.EffectiveCap(result.BaseCap, modifiers.Item1, modifiers.Item2) : double.PositiveInfinity;
                var boundary = Math.Min(cap, input.GateDistance);
                var dt = Math.Min(2, remaining);
                if (result.MovingSeconds < input.NextBuffExpiry)
                    dt = Math.Min(dt, input.NextBuffExpiry - result.MovingSeconds);
                dt = Math.Min(dt, Math.Max(0, boundary - result.Distance) / speed);
                var pressure = Math.Max(0, input.Pressure?.Invoke(result.Distance, runKills) ?? 1);
                var incoming = damage / seconds * pressure / referencePressure;
                // Zero-damage history does not establish immunity beyond observed threat.
                if (result.Distance > observedDistance && pressure > observedPressure && incoming <= 0)
                    incoming = (pressure - observedPressure) * killRate;
                var netDamage = incoming - input.Regen;
                if (netDamage > 0) dt = Math.Min(dt, health / netDamage);
                var step = new Step { Seconds = dt, Distance = result.Distance + speed * dt, DamageTaken = incoming * dt };
                result.TaskRemainder += taskRate * dt; result.KillRemainder += killRate * dt;
                step.Tasks = (int)Math.Floor(result.TaskRemainder); step.Kills = (int)Math.Floor(result.KillRemainder);
                result.TaskRemainder -= step.Tasks; result.KillRemainder -= step.Kills;
                result.Tasks += step.Tasks; result.Kills += step.Kills; runKills += step.Kills;
                health = Math.Clamp(health - netDamage * dt, 0, input.MaxHealth);
                remaining -= dt; result.Seconds += dt; result.MovingSeconds += dt; result.Distance = step.Distance;
                if (result.Distance >= input.GateDistance - .00001)
                { result.StopReason = "NPC encounter"; result.Steps.Add(step); break; }
                if (health <= .00001 || result.Distance >= cap - .00001)
                {
                    step.EndsRun = true; step.Reaped = health > .00001;
                    if (step.Reaped)
                    {
                        result.Reaps++;
                        if (input.CapGain) { step.CapIncrease = RunDistanceRules.ReapGain(cap, 1, 0); result.BaseCap += step.CapIncrease; }
                    }
                    else result.Deaths++;
                    if (step.Reaped && input.StopOnFirstReap)
                    { result.StopReason = "First reap dialogue"; result.Steps.Add(step); break; }
                    step.RestartSeconds = Math.Min(remaining, input.RestartSeconds);
                    remaining -= step.RestartSeconds; result.Seconds += step.RestartSeconds;
                    result.Steps.Add(step);
                    // Preserve a partly elapsed countdown, rather than granting a free restart.
                    if (step.RestartSeconds + .00001 < input.RestartSeconds) break;
                    health = input.MaxHealth; result.Distance = input.StartingDistance; runKills = 0;
                    continue;
                }
                result.Steps.Add(step);
                if (dt <= 0) break;
            }
            result.HealthFraction = health / input.MaxHealth;
            return result;
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0;
    }
}
