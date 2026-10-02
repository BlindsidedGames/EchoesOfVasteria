using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using Sirenix.Serialization;

[assembly: RegisterFormatter(typeof(Blindsided.SaveData.SkillProgressCompatibilityFormatter))]
namespace Blindsided.SaveData
{
    /// <summary>New writes omit obsolete purchases and cached eligibility. Legacy reads remain permissive.</summary>
    public static class CurrentSaveCodec
    {
        private static readonly ISerializationPolicy Policy = new CustomSerializationPolicy(
            "Echoes.CurrentSave.Schema4", true, member =>
                member is FieldInfo field && field.IsPublic && !field.IsNotSerialized &&
                !(field.DeclaringType == typeof(GameData) &&
                  (field.Name == nameof(GameData.UpgradeLevels) ||
                   field.Name == nameof(GameData.StatUpgradesMigratedToGear) ||
                   field.Name == nameof(GameData.DuckHelmetSanitized))));

        public static byte[] Serialize(GameData data)
        {
            var invalidProgress = Migrations.Migration_SchemaV4CurrentCollections.GetInvalidKnownProgressError(data);
            if (invalidProgress != null) throw new InvalidDataException(invalidProgress);
            var context = new SerializationContext();
            context.Config.SerializationPolicy = Policy;
            return SerializationUtility.SerializeValue(data, DataFormat.Binary, context);
        }

        // A legacy-preserving clone is used before migrations; the write projection must not
        // erase one-time guards before the historical migration runner has consumed them.
        public static GameData Clone(GameData data) => SerializationUtility.DeserializeValue<GameData>(
            SerializationUtility.SerializeValue(data, DataFormat.Binary), DataFormat.Binary);
    }

    /// <summary>Accepts string-era and typed milestone lists. Writes only deliberate active choices.</summary>
    public sealed class SkillProgressCompatibilityFormatter : MinimalBaseFormatter<GameData.SkillProgress>
    {
        protected override GameData.SkillProgress GetUninitializedObject() => new GameData.SkillProgress();
        protected override void Read(ref GameData.SkillProgress value, IDataReader reader)
        {
            while (reader.PeekEntry(out var name) != EntryType.EndOfNode && reader.PeekEntry(out _) != EntryType.EndOfStream)
            {
            switch (name)
            {
                case "Level": value.Level = Serializer.Get<int>().ReadValue(reader); break;
                case "CurrentXP": value.CurrentXP = Serializer.Get<float>().ReadValue(reader); break;
                case "Milestones":
                case "MilestoneRecords":
                case "ActiveMilestoneIds":
                    var records = Serializer.Get<object>().ReadValue(reader) as IEnumerable;
                    if (records == null) break;
                    foreach (var item in records)
                    {
                        if (item is GameData.MilestoneProgressRecord record && !string.IsNullOrWhiteSpace(record.Id))
                            value.Milestones.Add(record);
                        else if (name == "ActiveMilestoneIds" && item is string id && !string.IsNullOrWhiteSpace(id))
                            value.Milestones.Add(new GameData.MilestoneProgressRecord { Id = id, IsActive = true });
                        // Old string lists are automatic unlocks, not equipped choices.
                    }
                    break;
                default: reader.SkipEntry(); break;
            }
            }
        }
        protected override void Write(ref GameData.SkillProgress value, IDataWriter writer)
        {
            Serializer.Get<int>().WriteValue("Level", value.Level, writer);
            Serializer.Get<float>().WriteValue("CurrentXP", value.CurrentXP, writer);
            var active = new HashSet<string>(value.Milestones.Where(x => x != null && x.IsActive &&
                !string.IsNullOrWhiteSpace(x.Id)).Select(x => x.Id), StringComparer.Ordinal);
            Serializer.Get<HashSet<string>>().WriteValue("ActiveMilestoneIds", active, writer);
        }
    }
}
