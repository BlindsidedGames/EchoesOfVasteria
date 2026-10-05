using System;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Blindsided.SaveData
{
    /// <summary>Explicit import adapter. Never searches, selects or overwrites an old bank.</summary>
    public static class LegacyEs3Adapter
    {
        public static bool TryDecode(string json, out GameData data, out string error)
        {
            data = null; error = null;
            try
            {
                var bank = JObject.Parse(QuoteIntegerPropertyNames(json));
                JObject payload = null;
                var recognizedEntries = 0;
                foreach (var pair in bank.Properties())
                {
                    if (!(pair.Value is JObject wrapper)) continue;
                    var type = (string)wrapper["__type"];
                    if (type == null || !(type.StartsWith("GameData,", StringComparison.Ordinal) ||
                        type.StartsWith("Blindsided.SaveData.GameData,", StringComparison.Ordinal))) continue;
                    if (++recognizedEntries > 1) throw new InvalidOperationException("Multiple GameData entries; select a bank explicitly.");
                    payload = wrapper["value"] as JObject;
                    if (payload == null) throw new InvalidOperationException("Malformed ES3 GameData entry; its value must be an object.");
                }
                if (payload == null) throw new InvalidOperationException("No supported ES3 GameData entry.");
                // Investigated ES3 banks predate a schema field. Explicit schema 1 is also a
                // supported declaration; never relabel future/malformed data as an old save.
                if (payload.TryGetValue("SchemaVersion", out var declaredSchema) &&
                    (declaredSchema.Type != JTokenType.Integer || declaredSchema.Value<long>() != 1L))
                    throw new InvalidOperationException("Unsupported ES3 schema declaration; only absent schema or integer schema 1 is supported.");
                var normalized = (JObject)payload.DeepClone();
                if (normalized["SkillData"] is JObject skills)
                    foreach (var pair in skills.Properties())
                    {
                        if (!(pair.Value is JObject skill)) continue;
                        var records = skill["MilestoneRecords"] ?? skill["Milestones"];
                        skill.Remove("Milestones");
                        skill["MilestoneRecords"] = records is JArray list
                            ? new JArray(System.Linq.Enumerable.OfType<JObject>(list)) : new JArray();
                    }
                data = normalized.ToObject<GameData>();
                if (data == null) throw new InvalidOperationException("Empty legacy payload.");
                data.SchemaVersion = 1;
                // Preserve absent producer provenance; never use the importing app's version.
                if (payload["LastGameVersion"] == null) data.LastGameVersion = null;
                return true;
            }
            catch (Exception ex) { error = ex.Message; return false; }
        }

        // Easy Save wrote unquoted integer TaskRecords keys. Normalize only those property
        // names outside quoted strings; numeric values, float precision and escapes stay intact.
        private static string QuoteIntegerPropertyNames(string source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var output = new StringBuilder(source.Length);
            var inString = false;
            var escaped = false;
            for (var index = 0; index < source.Length; index++)
            {
                var character = source[index];
                output.Append(character);
                if (inString)
                {
                    if (escaped) escaped = false;
                    else if (character == '\\') escaped = true;
                    else if (character == '"') inString = false;
                    continue;
                }
                if (character == '"') { inString = true; continue; }
                if (character != '{' && character != ',') continue;

                var start = index + 1;
                var cursor = start;
                while (cursor < source.Length && char.IsWhiteSpace(source[cursor])) cursor++;
                var numberStart = cursor;
                if (cursor < source.Length && source[cursor] == '-') cursor++;
                var digitsStart = cursor;
                while (cursor < source.Length && source[cursor] >= '0' && source[cursor] <= '9') cursor++;
                if (cursor == digitsStart) continue;
                var numberEnd = cursor;
                while (cursor < source.Length && char.IsWhiteSpace(source[cursor])) cursor++;
                if (cursor >= source.Length || source[cursor] != ':') continue;

                output.Append(source, start, numberStart - start);
                output.Append('"').Append(source, numberStart, numberEnd - numberStart).Append('"');
                output.Append(source, numberEnd, cursor - numberEnd + 1);
                index = cursor;
            }
            return output.ToString();
        }
    }
}
