// Future DTO projection experiment, not production migration code.
using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sirenix.Serialization;
using Blindsided.SaveData;
class OmissionProbe
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    static int Main(string[] args)
    {
        UnityEngine.Debug.unityLogger.logEnabled = false;
        var legacy = JObject.Parse(File.ReadAllText(args[0]));
        var projected = (JObject)legacy.DeepClone();
        foreach (var field in new[] { "UpgradeLevels", "StatUpgradesMigratedToGear", "DuckHelmetSanitized" })
            projected.Remove(field);
        foreach (var skill in ((JObject)projected["SkillData"] ?? new JObject()).Properties())
        {
            var value = (JObject)skill.Value;
            var records = value["MilestoneRecords"] ?? value["Milestones"];
            value.Remove("Milestones");
            var selections = new JArray();
            foreach (var token in records as JArray ?? new JArray())
            {
                // Historical strings are automatic eligibility markers, not selections.
                if (token is JObject record && (bool?)record["IsActive"] == true)
                    selections.Add(record["Id"]?.DeepClone());
            }
            value.Remove("MilestoneRecords");
            value["ActiveMilestoneIds"] = selections;
        }
        // Proposed future schema only in generated model; no shipped schema reservation.
        projected["SchemaVersion"] = 4;
        var data = JsonConvert.DeserializeObject<GameData>(projected.ToString(),
            new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace });
        var payload = SerializationUtility.SerializeValue(data, DataFormat.Binary);
        var context = new DeserializationContext();
        context.Config.DebugContext.LoggingPolicy = LoggingPolicy.Silent;
        context.Config.DebugContext.ErrorHandlingPolicy = ErrorHandlingPolicy.ThrowOnErrors;
        var reload = SerializationUtility.DeserializeValue<GameData>(payload, DataFormat.Binary, context);
        // Normalize JSON numeric representations: FromObject keeps float-backed
        // JValues, whereas the compatible input was parsed as JSON doubles.
        var after = JObject.Parse(JsonConvert.SerializeObject(reload));
        var result = new JObject();
        foreach (var field in new[] { "UpgradeLevels", "StatUpgradesMigratedToGear", "DuckHelmetSanitized" })
        {
            Require(after[field] == null, "Field retained: " + field);
            Require(!Encoding.UTF8.GetString(payload).Contains(field), "Payload contains omitted field: " + field);
        }
        foreach (var field in new[] { "TierIndex", "MilestoneRecords", "IsActive" })
            Require(!Encoding.UTF8.GetString(payload).Contains(field), "Payload contains obsolete/cache field: " + field);
        int skills = 0, selectionsCount = 0, activeCount = 0;
        foreach (var skill in ((JObject)legacy["SkillData"] ?? new JObject()).Properties())
        {
            var expected = (JObject)skill.Value;
            var actual = reload.SkillData[skill.Name];
            Require(actual.Level == (int)expected["Level"], "Level changed");
            Require(BitConverter.GetBytes(actual.CurrentXP).SequenceEqual(
                BitConverter.GetBytes(expected["CurrentXP"].ToObject<float>())), "XP changed");
            var records = expected["MilestoneRecords"] ?? expected["Milestones"];
            var typed = (records as JArray ?? new JArray()).OfType<JObject>().ToList();
            Require(actual.ActiveMilestoneIds.SetEquals(typed.Where(t => (bool)t["IsActive"]).Select(t => (string)t["Id"])),
                "Active choice set changed");
            for (int i = 0; i < typed.Count; i++)
                Require(actual.ActiveMilestoneIds.Contains((string)typed[i]["Id"]) == (bool)typed[i]["IsActive"],
                    "Player choice value changed");
            skills++;
            selectionsCount += typed.Count;
            activeCount += actual.ActiveMilestoneIds.Count;
        }
        foreach (var field in new[] { "Resources", "Quests", "CompletedNpcTasks", "UnlockedBuffSlots",
            "UnlockedAutoBuffSlots", "BuffSlots", "AutoBuffSlots", "TaskRecords", "Disciples", "EquipmentBySlot",
            "CauldronCards", "CauldronStew", "ResourceStats", "MapStats", "General", "Forge" })
        {
            // Compare to the compatible old DTO, not absent fields versus constructor defaults.
            if (legacy[field] != null)
            {
                Require(JToken.DeepEquals(legacy[field], after[field]), "Meaningful field changed: " + field);
                result[field + "Equal"] = true;
            }
        }
        result["skillCount"] = skills;
        result["typedIdActivePairsEqual"] = selectionsCount;
        result["persistedActiveIds"] = activeCount;
        result["levelXpFloatBitsEqual"] = true;
        result["obsoleteFieldsAbsentFromObjectAndPayload"] = true;
        result["schema"] = reload.SchemaVersion;
        File.WriteAllBytes(args[1] + ".payload", payload);
        File.WriteAllText(args[1] + ".json", result.ToString());
        Console.WriteLine(result.ToString(Formatting.None));
        return 0;
    }
}
