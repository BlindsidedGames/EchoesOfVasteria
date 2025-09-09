using System;
using System.Collections.Generic;
using System.IO;
using Blindsided.Utilities;
using UnityEngine;

namespace Blindsided.SaveData.Migrations
{
    /// <summary>
    /// Orchestrates ordered, idempotent migrations between saved versions/schemas.
    /// - Backs up current snapshot before first migration.
    /// - Applies schema migrations (ascending), then version migrations (ascending).
    /// - On success, updates LastGameVersion and SchemaVersion (if applicable) and persists once.
    /// </summary>
    public static class SaveMigrationRunner
    {
        private static readonly List<ISaveMigration> Registered = new List<ISaveMigration>();
        private static bool defaultsRegistered;

        public static void Register(ISaveMigration migration)
        {
            if (migration == null) return;
            Registered.Add(migration);
        }

        private static void EnsureDefaults()
        {
            if (defaultsRegistered) return;
            defaultsRegistered = true;
            // Built-in migrations
            Register(new Migration_DuckHelmetSanitation());
            Register(new Migration_CauldronOverflowRedistribution());
            Register(new Migration_GearAffixQuality());
        }

        /// <summary>
        /// Run applicable migrations for the provided data. Returns true if any migration ran.
        /// </summary>
        public static bool Run(GameData data, string toVersion, string currentSlotName)
        {
            if (data == null) return false;
            EnsureDefaults();

            var fromVersion = !string.IsNullOrWhiteSpace(data.LastGameVersion)
                ? data.LastGameVersion
                : (!string.IsNullOrWhiteSpace(data.GameVersionCreated) ? data.GameVersionCreated : "0.0.0");

            var toVer = string.IsNullOrWhiteSpace(toVersion) ? Application.version : toVersion;

            // Prepare lists
            var schemaMigrations = new List<ISaveMigration>();
            var versionMigrations = new List<ISaveMigration>();

            foreach (var m in Registered)
            {
                if (m == null) continue;
                if (m.TargetSchema.HasValue && data.SchemaVersion < m.TargetSchema.Value)
                {
                    schemaMigrations.Add(m);
                    continue;
                }
                if (!string.IsNullOrWhiteSpace(m.TargetVersion))
                {
                    // select when from < target <= to
                    if (VersionUtil.Compare(fromVersion, m.TargetVersion) < 0 &&
                        VersionUtil.Compare(m.TargetVersion, toVer) <= 0)
                    {
                        versionMigrations.Add(m);
                    }
                }
            }

            // Sort
            schemaMigrations.Sort((a, b) => a.TargetSchema.GetValueOrDefault().CompareTo(b.TargetSchema.GetValueOrDefault()));
            versionMigrations.Sort((a, b) => VersionUtil.Compare(a.TargetVersion, b.TargetVersion));

            if (schemaMigrations.Count == 0 && versionMigrations.Count == 0)
                return false;

            // Backup once before mutating
            TryBackupCurrentSnapshot(currentSlotName, "migrated");

            var appliedAny = false;
            var newSchema = data.SchemaVersion;

            try
            {
                // Apply schema migrations
                foreach (var m in schemaMigrations)
                {
                    SafeApply(m, data);
                    appliedAny = true;
                    newSchema = Math.Max(newSchema, m.TargetSchema.GetValueOrDefault(newSchema));
                }

                // Apply version migrations
                foreach (var m in versionMigrations)
                {
                    SafeApply(m, data);
                    appliedAny = true;
                }

                if (appliedAny)
                {
                    if (string.IsNullOrEmpty(data.GameVersionCreated))
                        data.GameVersionCreated = toVer;
                    data.LastGameVersion = toVer;
                    data.SchemaVersion = newSchema;

                    // Persist immediately to avoid re-entry and to lock in migrated state
                    try
                    {
                        Blindsided.SaveData.SaveManager.Instance.SaveAsync(data).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Migration persist failed: {ex}");
                        Blindsided.Utilities.FeedbackForm.SubmitException(
                            "Save.MigrationPersist",
                            ex,
                            $"slot: {currentSlotName}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Migration run aborted due to exception: {ex}");
                Blindsided.Utilities.FeedbackForm.SubmitException(
                    "Save.MigrationRun",
                    ex,
                    $"slot: {currentSlotName}");
                // do not rethrow to avoid preventing the game from loading
            }

            return appliedAny;
        }

        private static void SafeApply(ISaveMigration migration, GameData data)
        {
            try
            {
                migration.Apply(data);
                Debug.Log($"Applied save migration: {migration.Id}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Migration '{migration.Id}' threw an exception: {ex}");
                Blindsided.Utilities.FeedbackForm.SubmitException(
                    "Save.Migration",
                    ex,
                    migration.Id);
                throw;
            }
        }

        private static void TryBackupCurrentSnapshot(string slotName, string reason)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(slotName)) slotName = "Save1";
                var root = Application.persistentDataPath;
                var slotDir = Path.Combine(root, "Saves", slotName);
                var snapshot = Path.Combine(slotDir, "snapshot.bin");
                if (!File.Exists(snapshot)) return;

                var archiveDir = Path.Combine(slotDir, "Archive");
                Directory.CreateDirectory(archiveDir);
                var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var baseName = $"snapshot_{reason}_{stamp}.bin";
                var dest = Path.Combine(archiveDir, baseName);
                var i = 0;
                while (File.Exists(dest))
                {
                    i++;
                    dest = Path.Combine(archiveDir, $"snapshot_{reason}_{stamp}_{i}.bin");
                }
                File.Copy(snapshot, dest, overwrite: false);
                Debug.Log($"Backed up save snapshot to '{dest}' before migrations.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to back up snapshot before migration: {ex.Message}");
            }
        }
    }
}
