using System;
using System.Collections.Generic;
using System.Linq;
using Blindsided.Utilities;
using Sirenix.Serialization;
using UnityEngine;

namespace Blindsided.SaveData.Migrations
{
    public sealed class SaveMigrationResult
    {
        internal SaveMigrationResult(
            bool succeeded,
            bool changed,
            GameData data,
            IReadOnlyList<string> appliedIds,
            string error,
            IReadOnlyList<string> warnings = null)
        {
            Succeeded = succeeded;
            Changed = changed;
            Data = data;
            AppliedIds = appliedIds ?? Array.Empty<string>();
            Error = error;
            Warnings = warnings ?? Array.Empty<string>();
        }

        public bool Succeeded { get; }
        public bool Changed { get; }
        public GameData Data { get; }
        public IReadOnlyList<string> AppliedIds { get; }
        public string Error { get; }
        public IReadOnlyList<string> Warnings { get; }
    }

    /// <summary>
    /// Applies ordered save migrations to a detached copy. It never reads, moves, backs up, or writes files.
    /// A failed migration returns the original object unchanged and discards its partially changed candidate.
    /// </summary>
    public static class SaveMigrationRunner
    {
        private const int AppliedMigrationLedgerSchemaVersion = 2;
        private static readonly List<ISaveMigration> Registered = new();
        private static bool defaultsRegistered;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Registered.Clear();
            defaultsRegistered = false;
        }

        public static void Register(ISaveMigration migration)
        {
            if (migration == null || Registered.Any(item => item != null && item.Id == migration.Id))
                return;

            Registered.Add(migration);
        }

        public static SaveMigrationResult TryMigrate(GameData source, string toVersion)
        {
            if (source == null)
                return Failed(null, "Cannot migrate null save data.");
            if (source.SchemaVersion < 0)
                return Failed(source, $"Save schema {source.SchemaVersion} cannot be negative.");
            if (source.SchemaVersion > GameData.CurrentSchemaVersion)
            {
                return Failed(
                    source,
                    $"Save schema {source.SchemaVersion} is newer than supported schema {GameData.CurrentSchemaVersion}.");
            }
            var invalidProgress = Migration_SchemaV4CurrentCollections.GetInvalidKnownProgressError(source);
            if (invalidProgress != null)
                return Failed(source, invalidProgress);

            EnsureDefaults();
            var targetVersion = string.IsNullOrWhiteSpace(toVersion) ? Application.version : toVersion;
            var fromVersion = !string.IsNullOrWhiteSpace(source.LastGameVersion)
                ? source.LastGameVersion
                : (!string.IsNullOrWhiteSpace(source.GameVersionCreated) ? source.GameVersionCreated : "0.0.0");
            var alreadyApplied = source.AppliedMigrationIds ?? new HashSet<string>();
            var hasAppliedMigrationLedger = source.SchemaVersion >= AppliedMigrationLedgerSchemaVersion;
            var warnings = GetCompatibilityWarnings(source);

            var schemaMigrations = Registered
                .Where(migration => migration?.TargetSchema != null &&
                                    (source.SchemaVersion < migration.TargetSchema.Value ||
                                     (source.SchemaVersion == migration.TargetSchema.Value &&
                                      (!alreadyApplied.Contains(migration.Id) ||
                                       (migration is IConditionalRepairSaveMigration repair && repair.NeedsRepair(source))))))
                .OrderBy(migration => migration.TargetSchema.Value)
                .ThenBy(migration => migration.Id, StringComparer.Ordinal)
                .ToList();

            var versionMigrations = Registered
                .Where(migration => migration != null &&
                                     !migration.TargetSchema.HasValue &&
                                     !string.IsNullOrWhiteSpace(migration.TargetVersion) &&
                                     !alreadyApplied.Contains(migration.Id) &&
                                     (hasAppliedMigrationLedger ||
                                      VersionUtil.Compare(fromVersion, migration.TargetVersion) < 0) &&
                                     VersionUtil.Compare(migration.TargetVersion, targetVersion) <= 0)
                .OrderBy(migration => migration.TargetVersion, Comparer<string>.Create(VersionUtil.Compare))
                .ThenBy(migration => migration.Id, StringComparer.Ordinal)
                .ToList();

            // Schema 1 predates the applied-ID ledger. LastGameVersion is the durable evidence that
            // these historical version transitions already happened; record that fact without
            // replaying balance-sensitive transformations against newer assets.
            var historicalVersionMigrations = Registered
                .Where(migration => migration != null &&
                                    !hasAppliedMigrationLedger &&
                                    !migration.TargetSchema.HasValue &&
                                    !string.IsNullOrWhiteSpace(migration.TargetVersion) &&
                                    !alreadyApplied.Contains(migration.Id) &&
                                    VersionUtil.Compare(migration.TargetVersion, fromVersion) <= 0 &&
                                    VersionUtil.Compare(migration.TargetVersion, targetVersion) <= 0)
                .OrderBy(migration => migration.TargetVersion, Comparer<string>.Create(VersionUtil.Compare))
                .ThenBy(migration => migration.Id, StringComparer.Ordinal)
                .ToList();

            if (schemaMigrations.Count == 0 && versionMigrations.Count == 0 &&
                historicalVersionMigrations.Count == 0)
                return new SaveMigrationResult(true, false, source, Array.Empty<string>(), null, warnings);

            GameData candidate;
            try
            {
                var bytes = SerializationUtility.SerializeValue(source, DataFormat.Binary);
                candidate = SerializationUtility.DeserializeValue<GameData>(bytes, DataFormat.Binary);
                if (candidate == null)
                    return Failed(source, "Could not create a detached migration candidate.");
            }
            catch (Exception ex)
            {
                return Failed(source, $"Could not clone save data before migration: {ex}");
            }

            candidate.AppliedMigrationIds ??= new HashSet<string>();
            var appliedIds = new List<string>();

            try
            {
                foreach (var migration in schemaMigrations.Where(item => !(item is IPostVersionSaveMigration)))
                {
                    Apply(migration, candidate, appliedIds);
                    candidate.SchemaVersion = Math.Max(candidate.SchemaVersion, migration.TargetSchema.Value);
                }

                foreach (var migration in historicalVersionMigrations)
                    candidate.AppliedMigrationIds.Add(migration.Id);

                foreach (var migration in versionMigrations)
                    Apply(migration, candidate, appliedIds);

                foreach (var migration in schemaMigrations.Where(item => item is IPostVersionSaveMigration))
                {
                    Apply(migration, candidate, appliedIds);
                    candidate.SchemaVersion = Math.Max(candidate.SchemaVersion, migration.TargetSchema.Value);
                }

                if (candidate.SchemaVersion > GameData.CurrentSchemaVersion)
                {
                    return Failed(
                        source,
                        $"Migration produced unsupported schema {candidate.SchemaVersion}.");
                }

                if (string.IsNullOrWhiteSpace(candidate.GameVersionCreated))
                    candidate.GameVersionCreated = targetVersion;
                candidate.LastGameVersion = targetVersion;

                return new SaveMigrationResult(true, true, candidate, appliedIds, null, warnings);
            }
            catch (Exception ex)
            {
                return Failed(source, $"Save migration failed; the original data was not changed: {ex}");
            }
        }

#if UNITY_INCLUDE_TESTS
        public static SaveMigrationResult RunRollbackProbeForTests(GameData source)
        {
            var probe = new Migration_ThrowingRollbackProbe();
            Register(probe);
            try
            {
                return TryMigrate(source, probe.TargetVersion);
            }
            finally
            {
                Registered.RemoveAll(migration => migration?.Id == probe.Id);
            }
        }
#endif

        private static void Apply(ISaveMigration migration, GameData candidate, List<string> appliedIds)
        {
            migration.Apply(candidate);
            candidate.AppliedMigrationIds.Add(migration.Id);
            appliedIds.Add(migration.Id);
        }

        private static SaveMigrationResult Failed(GameData original, string error)
        {
            return new SaveMigrationResult(false, false, original, Array.Empty<string>(), error);
        }

        private static IReadOnlyList<string> GetCompatibilityWarnings(GameData source)
        {
            var warnings = new List<string>();
            var hasLimitedCards = source.CauldronCardCounts?.Any(pair => pair.Value > 0 &&
                (pair.Key?.StartsWith("RES:", StringComparison.Ordinal) == true ||
                 pair.Key?.StartsWith("BUFF:", StringComparison.Ordinal) == true)) == true;
            if (hasLimitedCards && !LegacyCauldronProfile.TryResolve(source, out _))
            {
                warnings.Add("Cauldron redistribution deferred: the source producer has no verified " +
                             "card-threshold profile. Existing card counts were preserved.");
            }
            if (hasLimitedCards && source.SchemaVersion >= 2 &&
                source.AppliedMigrationIds?.Contains("SchemaV2ZZCauldronOverflowRepair") == true &&
                source.AppliedMigrationIds.Contains("SchemaV4LegacyCompatibility") == false)
            {
                warnings.Add("A prior Cauldron repair receipt exists. Any earlier redistribution " +
                             "under incorrect thresholds cannot be reconstructed from aggregate " +
                             "Infinity balances; compare a premigration backup before recovery.");
            }
            return warnings;
        }

        private static void EnsureDefaults()
        {
            if (defaultsRegistered)
                return;

            defaultsRegistered = true;
            Register(new Migration_SchemaV2Normalization());
            Register(new Migration_GatheringBuffs());
            Register(new Migration_SchemaV2CauldronOverflowRepair());
            Register(new Migration_DuckHelmetSanitation());
            Register(new Migration_CauldronOverflowRedistribution());
            Register(new Migration_GearAffixQuality());
            Register(new Migration_SchemaV4LegacyCompatibility());
            Register(new Migration_SchemaV4CurrentCollections());
        }
    }

#if UNITY_INCLUDE_TESTS
    internal sealed class Migration_ThrowingRollbackProbe : ISaveMigration
    {
        public int? TargetSchema => null;
        public string TargetVersion => "9999.0.0";
        public string Id => "Test_ThrowingRollbackProbe";

        public void Apply(GameData data)
        {
            data.CompletionPercentage = 999f;
            throw new InvalidOperationException("Intentional migration rollback probe.");
        }
    }
#endif

    internal sealed class Migration_SchemaV2Normalization : ISaveMigration
    {
        public int? TargetSchema => 2;
        public string TargetVersion => null;
        public string Id => "SchemaV2Normalization";

        public void Apply(GameData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            data.AppliedMigrationIds ??= new HashSet<string>();
            data.SavedPreferences ??= new GameData.Preferences();
            data.SkillData ??= new Dictionary<string, GameData.SkillProgress>();
            data.UpgradeLevels ??= new Dictionary<string, int>();
            data.Resources ??= new Dictionary<string, GameData.ResourceEntry>();
            data.EnemyKills ??= new Dictionary<string, double>();
            data.CompletedNpcTasks ??= new HashSet<string>();
            data.Disciples ??= new Dictionary<string, GameData.DiscipleGenerationRecord>();
            data.Quests ??= new Dictionary<string, GameData.QuestRecord>();
            data.RetroQuestRewardsApplied ??= new HashSet<string>();
            data.EquipmentBySlot ??= new Dictionary<string, GearItemRecord>();
            data.PinnedQuests ??= new List<string>();
            data.Forge ??= new GameData.ForgeStats();
            data.TaskRecords ??= new Dictionary<int, GameData.TaskRecord>();
            data.ResourceStats ??= new Dictionary<string, GameData.ResourceRecord>();
            data.MapStats ??= new Dictionary<string, GameData.MapStatistics>();
            data.General ??= new GameData.GeneralStats();
            data.CauldronCardCounts ??= new Dictionary<string, int>();
            data.CauldronTotals ??= new GameData.CauldronTotalsRecord();
            data.Farm ??= new TimelessEchoes.Farming.FarmState();

            NormalizePreferences(data.SavedPreferences);
            NormalizeSkills(data.SkillData);
            NormalizeResources(data.Resources);
            NormalizeBuffSlots(data);
            NormalizeDisciples(data.Disciples);
            NormalizeQuests(data.Quests);
            NormalizeEquipment(data.EquipmentBySlot);
            NormalizeReferenceValues(data.TaskRecords, () => new GameData.TaskRecord());
            NormalizeReferenceValues(data.ResourceStats, () => new GameData.ResourceRecord());
            NormalizeReferenceValues(data.MapStats, () => new GameData.MapStatistics());
            NormalizeGeneralStats(data.General);
            NormalizeForge(data.Forge);
            NormalizeCanonicalStats(data);

            if (data.UnlockedBuffSlots <= 0)
                data.UnlockedBuffSlots = 1;
            else if (data.UnlockedBuffSlots > 5)
                data.UnlockedBuffSlots = 5;
            data.UnlockedAutoBuffSlots = Math.Max(0, Math.Min(5, data.UnlockedAutoBuffSlots));
            if (!(data.DisciplePercent > 0f) || float.IsInfinity(data.DisciplePercent))
                data.DisciplePercent = 0.01f;
            if (data.CauldronEvaLevel <= 0)
                data.CauldronEvaLevel = 1;
            if (!(data.OfflineTimeCap > 0d) || double.IsInfinity(data.OfflineTimeCap))
                data.OfflineTimeCap = 3600d;
            if (!(data.OfflineTimeScaleMultiplier > 0d) || double.IsInfinity(data.OfflineTimeScaleMultiplier))
                data.OfflineTimeScaleMultiplier = 2d;
        }

        private static void NormalizePreferences(GameData.Preferences preferences)
        {
            preferences.Foldouts ??= new Dictionary<string, bool>();
            preferences.IngotCraftAmount = PositiveOrDefault(preferences.IngotCraftAmount, 1d);
            preferences.CrystalCraftAmount = PositiveOrDefault(preferences.CrystalCraftAmount, 1d);
            preferences.ChunkCraftAmount = PositiveOrDefault(preferences.ChunkCraftAmount, 1d);
            preferences.CoreCraftAmount = PositiveOrDefault(preferences.CoreCraftAmount, 1d);
        }

        private static void NormalizeSkills(Dictionary<string, GameData.SkillProgress> skills)
        {
            foreach (var key in skills.Keys.ToList())
            {
                if (!Migration_SchemaV4CurrentCollections.IsKnownSkillKey(key))
                    continue;
                var progress = skills[key] ?? new GameData.SkillProgress();
                if (progress.Level <= 0)
                    progress.Level = 1;
                progress.NormalizeMilestones();
                skills[key] = progress;
            }
        }

        private static void NormalizeResources(Dictionary<string, GameData.ResourceEntry> resources)
        {
            NormalizeReferenceValues(resources, () => new GameData.ResourceEntry(), entry =>
            {
                if (entry.Tier <= 0)
                    entry.Tier = 1;
            });
        }

        private static void NormalizeBuffSlots(GameData data)
        {
            data.BuffSlots ??= new List<string> { "Echo Tasks", null, null, null, null };
            while (data.BuffSlots.Count < 5)
                data.BuffSlots.Add(null);

            data.AutoBuffSlots ??= new List<bool> { false, false, false, false, false };
            while (data.AutoBuffSlots.Count < 5)
                data.AutoBuffSlots.Add(false);
        }

        private static void NormalizeDisciples(
            Dictionary<string, GameData.DiscipleGenerationRecord> disciples)
        {
            NormalizeReferenceValues(disciples, () => new GameData.DiscipleGenerationRecord(), record =>
            {
                record.StoredResources ??= new Dictionary<string, double>();
                record.TotalCollected ??= new Dictionary<string, double>();
                NormalizeFiniteNonNegativeValues(record.StoredResources);
                NormalizeFiniteNonNegativeValues(record.TotalCollected);
                if (!(record.Progress >= 0f) || float.IsNaN(record.Progress) || float.IsInfinity(record.Progress))
                    record.Progress = 0f;
                if (!(record.LastGenerationTime >= 0d) || double.IsNaN(record.LastGenerationTime) ||
                    double.IsInfinity(record.LastGenerationTime))
                    record.LastGenerationTime = 0d;
            });
        }

        private static void NormalizeFiniteNonNegativeValues(Dictionary<string, double> values)
        {
            foreach (var key in values.Keys.ToList())
            {
                var value = values[key];
                if (!(value >= 0d) || double.IsNaN(value) || double.IsInfinity(value))
                    values[key] = 0d;
            }
        }

        private static void NormalizeQuests(Dictionary<string, GameData.QuestRecord> quests)
        {
            NormalizeReferenceValues(quests, () => new GameData.QuestRecord(), record =>
            {
                record.KillProgress ??= new Dictionary<string, double>();
                record.BuffCastProgress ??= new Dictionary<string, int>();
            });
        }

        private static void NormalizeEquipment(Dictionary<string, GearItemRecord> equipment)
        {
            NormalizeReferenceValues(equipment, () => new GearItemRecord(), record =>
            {
                record.affixes ??= new List<GearAffixRecord>();
            });
        }

        private static void NormalizeGeneralStats(GameData.GeneralStats general)
        {
            general.RecentRuns ??= new List<GameData.RunRecord>();
            general.RecentRuns.RemoveAll(run => run == null);
            if (!(general.MaxRunDistance > 0f) || float.IsInfinity(general.MaxRunDistance))
                general.MaxRunDistance = 50f;
            if (general.NextRunNumber <= 0)
                general.NextRunNumber = 1;
        }

        private static void NormalizeForge(GameData.ForgeStats forge)
        {
            forge.ResourcesSpent ??= new Dictionary<string, double>();
            forge.ResourcesGainedFromSalvage ??= new Dictionary<string, double>();
            forge.CoresSpentByCore ??= new Dictionary<string, double>();
            forge.IngotsSpentByCore ??= new Dictionary<string, double>();
            forge.CraftsByCore ??= new Dictionary<string, int>();
            forge.CraftsBySlot ??= new Dictionary<string, int>();
            forge.CraftsByRarity ??= new Dictionary<string, int>();
            forge.RarityCountsByCore ??= new Dictionary<string, Dictionary<string, int>>();
            forge.SlotCountsByCore ??= new Dictionary<string, Dictionary<string, int>>();
            forge.AffixCountDistribution ??= new Dictionary<int, int>();
            forge.UpgradesBySlot ??= new Dictionary<string, int>();
            forge.UpgradesByRarity ??= new Dictionary<string, int>();
            forge.UpgradeScoreDeltaBySlot ??= new Dictionary<string, GameData.ForgeStats.FloatAgg>();
            forge.StatRolls ??= new Dictionary<string, GameData.ForgeStats.StatAgg>();
            forge.StatRollsByRarity ??=
                new Dictionary<string, Dictionary<string, GameData.ForgeStats.StatAgg>>();
            forge.StatRollsBySlot ??=
                new Dictionary<string, Dictionary<string, GameData.ForgeStats.StatAgg>>();
            forge.HighRollsByStat ??= new Dictionary<string, int>();
            forge.CumulativeStatTotalsByStat ??= new Dictionary<string, double>();
            forge.HighestRollByStat ??= new Dictionary<string, float>();
            forge.IvanXpByCore ??= new Dictionary<string, double>();
            forge.IvanXpByRarity ??= new Dictionary<string, double>();
            forge.AutocraftStopReasons ??= new Dictionary<string, int>();
            forge.AutocraftBestRarityTierBySlot ??= new Dictionary<string, int>();
            forge.SalvagesByRarity ??= new Dictionary<string, int>();
            forge.SalvagesByCore ??= new Dictionary<string, int>();
            forge.SalvageYieldPerResource ??=
                new Dictionary<string, GameData.ForgeStats.ResourceAgg>();
            forge.ConversionSpentByResource ??= new Dictionary<string, double>();
            forge.CrystalsCraftedByResource ??= new Dictionary<string, double>();
            forge.ChunksCraftedByResource ??= new Dictionary<string, double>();
            forge.IngotsCraftedByResource ??= new Dictionary<string, double>();
            forge.CoresCraftedByResource ??= new Dictionary<string, double>();
            forge.BestPieceScoreBySlot ??= new Dictionary<string, float>();
            forge.BestPieceScoreByCore ??= new Dictionary<string, float>();
            forge.MinPieceScoreByCore ??= new Dictionary<string, float>();
            forge.MaxPieceScoreByCore ??= new Dictionary<string, float>();
            forge.BestPieceScoreByRarity ??= new Dictionary<string, float>();
            forge.BestAbsolutePieceScoreBySlot ??= new Dictionary<string, float>();
            forge.BestAbsolutePieceScoreByCore ??= new Dictionary<string, float>();
            forge.BestAbsolutePieceScoreByRarity ??= new Dictionary<string, float>();
            forge.BestAbsolutePieceSlotByCore ??= new Dictionary<string, string>();
            forge.BestAbsolutePieceSlotByRarity ??= new Dictionary<string, string>();
            forge.EquipsBySlot ??= new Dictionary<string, int>();
            forge.SalvagesBySlot ??= new Dictionary<string, int>();
            forge.CraftsBySlotTotals ??= new Dictionary<string, int>();

            NormalizeReferenceValues(
                forge.RarityCountsByCore,
                () => new Dictionary<string, int>());
            NormalizeReferenceValues(
                forge.SlotCountsByCore,
                () => new Dictionary<string, int>());
            NormalizeReferenceValues(
                forge.UpgradeScoreDeltaBySlot,
                () => new GameData.ForgeStats.FloatAgg());
            NormalizeReferenceValues(
                forge.StatRolls,
                () => new GameData.ForgeStats.StatAgg());
            NormalizeReferenceValues(
                forge.StatRollsByRarity,
                () => new Dictionary<string, GameData.ForgeStats.StatAgg>(),
                rolls => NormalizeReferenceValues(
                    rolls,
                    () => new GameData.ForgeStats.StatAgg()));
            NormalizeReferenceValues(
                forge.StatRollsBySlot,
                () => new Dictionary<string, GameData.ForgeStats.StatAgg>(),
                rolls => NormalizeReferenceValues(
                    rolls,
                    () => new GameData.ForgeStats.StatAgg()));
            NormalizeReferenceValues(
                forge.SalvageYieldPerResource,
                () => new GameData.ForgeStats.ResourceAgg());

            if (!(forge.HighRollTopPercentThreshold > 0f &&
                  forge.HighRollTopPercentThreshold <= 1f))
            {
                forge.HighRollTopPercentThreshold = 0.9f;
            }
        }

        private static void NormalizeReferenceValues<TKey, TValue>(
            Dictionary<TKey, TValue> values,
            Func<TValue> create,
            Action<TValue> normalize = null)
            where TValue : class
        {
            foreach (var key in values.Keys.ToList())
            {
                var value = values[key] ?? create();
                normalize?.Invoke(value);
                values[key] = value;
            }
        }

        private static double PositiveOrDefault(double value, double fallback)
        {
            return value > 0d && !double.IsInfinity(value) ? value : fallback;
        }

        private static void NormalizeCanonicalStats(GameData data)
        {
#pragma warning disable 618
            if (data.General.DistanceTravelledDouble == 0 && data.General.DistanceTravelled != 0)
                data.General.DistanceTravelledDouble = data.General.DistanceTravelled;
            if (data.General.DamageDealtDouble == 0 && data.General.DamageDealt != 0)
                data.General.DamageDealtDouble = data.General.DamageDealt;
            if (data.General.DamageTakenDouble == 0 && data.General.DamageTaken != 0)
                data.General.DamageTakenDouble = data.General.DamageTaken;

            foreach (var run in data.General.RecentRuns.Where(run => run != null))
            {
                if (run.DamageDealtDouble == 0 && run.DamageDealt != 0)
                    run.DamageDealtDouble = run.DamageDealt;
                if (run.DamageTakenDouble == 0 && run.DamageTaken != 0)
                    run.DamageTakenDouble = run.DamageTaken;
            }

            foreach (var key in data.MapStats.Keys.ToList())
            {
                var stats = data.MapStats[key] ?? new GameData.MapStatistics();
                if (stats.StepsDouble == 0 && stats.Steps != 0)
                    stats.StepsDouble = stats.Steps;
                if (stats.LongestTrekDouble == 0 && stats.LongestTrek != 0)
                    stats.LongestTrekDouble = stats.LongestTrek;
                if (stats.DamageDealtDouble == 0 && stats.DamageDealt != 0)
                    stats.DamageDealtDouble = stats.DamageDealt;
                if (stats.DamageTakenDouble == 0 && stats.DamageTaken != 0)
                    stats.DamageTakenDouble = stats.DamageTaken;
                data.MapStats[key] = stats;
            }
#pragma warning restore 618
        }
    }

    /// <summary>
    /// Repairs the known cauldron overflow shape on schema-1 saves without replaying every
    /// historical version migration under newer balance assets.
    /// </summary>
    internal sealed class Migration_SchemaV2CauldronOverflowRepair : ISaveMigration
    {
        public int? TargetSchema => 2;
        public string TargetVersion => null;
        public string Id => "SchemaV2ZZCauldronOverflowRepair";

        public void Apply(GameData data)
        {
            new Migration_CauldronOverflowRedistribution().Apply(data);
        }
    }
}
