// Investigation-only adapter. No migrations, asset loading, or SaveManager writes.
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sirenix.Serialization;
using Blindsided.SaveData;
class LegacySkillsProbe
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    static bool SameFloat(float a, float b) { return BitConverter.GetBytes(a).SequenceEqual(BitConverter.GetBytes(b)); }
    static int Main(string[] args)
    {
        UnityEngine.Debug.unityLogger.logEnabled = false;
        var original = JObject.Parse(File.ReadAllText(args[0]));
        var candidate = (JObject)original.DeepClone();
        var archive = new JObject();
        foreach (var skill in ((JObject)candidate["SkillData"] ?? new JObject()).Properties())
        {
            var progress = (JObject)skill.Value;
            archive[skill.Name] = progress["Milestones"]?.DeepClone();
            progress.Remove("Milestones");
            progress["MilestoneRecords"] = new JArray();
        }
        // ES3 has no schema field. Never inherit the new constructor's schema 3.
        candidate["SchemaVersion"] = 1;
        var settings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };
        var data = JsonConvert.DeserializeObject<GameData>(candidate.ToString(), settings);
        var bytes = SerializationUtility.SerializeValue(data, DataFormat.Binary);
        var context = new DeserializationContext();
        context.Config.DebugContext.LoggingPolicy = LoggingPolicy.Silent;
        context.Config.DebugContext.ErrorHandlingPolicy = ErrorHandlingPolicy.ThrowOnErrors;
        var reloaded = SerializationUtility.DeserializeValue<GameData>(bytes, DataFormat.Binary, context);
        var checks = new JObject();
        int count = 0;
        foreach (var skill in ((JObject)original["SkillData"] ?? new JObject()).Properties())
        {
            var expected = (JObject)skill.Value;
            var actual = reloaded.SkillData[skill.Name];
            Require(actual.Level == (int)expected["Level"], "Level changed: " + skill.Name);
            Require(SameFloat(actual.CurrentXP, expected["CurrentXP"].ToObject<float>()), "Float bits changed: " + skill.Name);
            Require(actual.MilestoneRecords.Count == 0, "Invented typed milestones");
            count++;
        }
        Require(reloaded.SchemaVersion == 1, "Skipped schema ancestry");
        Require(data.LastGameVersion == reloaded.LastGameVersion, "Invented producer version");
        Require(data.GameVersionCreated == reloaded.GameVersionCreated, "Invented creation version");
        foreach (var field in new[] { "Resources", "Quests", "TaskRecords", "UpgradeLevels", "Disciples" })
        {
            var before = JObject.FromObject(data)[field];
            var after = JObject.FromObject(reloaded)[field];
            Require(JToken.DeepEquals(before, after), "Roundtrip changed " + field);
            checks[field + "RoundtripEqual"] = true;
        }
        checks["skillLevelAndXpFloatBitsEqual"] = true;
        checks["skillCount"] = count;
        checks["stringMilestoneArchive"] = archive;
        checks["schema"] = reloaded.SchemaVersion;
        checks["lastVersion"] = reloaded.LastGameVersion;
        checks["createdVersion"] = reloaded.GameVersionCreated;
        File.WriteAllBytes(args[1] + ".payload", bytes);
        File.WriteAllText(args[1] + ".json", checks.ToString());
        Console.WriteLine("skills=" + count + " exact level/XP bits; known dictionaries roundtrip equal");
        return 0;
    }
}
