// Isolated command-line investigation. Reads a COPY of a historic snapshot, never SaveManager.
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Reflection;
using Sirenix.Serialization;
using Newtonsoft.Json;
using Blindsided.SaveData;
class PayloadProbe
{
    static int Main(string[] args)
    {
        try
        {
            // Odin's architecture initializer logs through Unity. Disable that managed logger
            // in this command-line process; no Player or Editor is started.
            UnityEngine.Debug.unityLogger.logEnabled = false;
            using (var stream = File.OpenRead(args[0]))
            using (var reader = new BinaryReader(stream))
            {
                var schema = reader.ReadInt32();
                var time = DateTime.FromBinary(reader.ReadInt64());
                var build = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadUInt16()));
                var size = reader.ReadInt32();
                reader.ReadBytes(reader.ReadUInt16());
                if (size != stream.Length - stream.Position) throw new Exception("Payload length mismatch");
                var context = new DeserializationContext();
                context.Config.DebugContext.LoggingPolicy = LoggingPolicy.Silent;
                context.Config.DebugContext.ErrorHandlingPolicy = ErrorHandlingPolicy.ThrowOnErrors;
                var data = SerializationUtility.DeserializeValue<GameData>(reader.ReadBytes(size), DataFormat.Binary, context);
                if (data == null) throw new Exception("Null payload");
                Console.WriteLine("headerSchema=" + schema + " build=" + build + " time=" + time.ToUniversalTime().ToString("o"));
                Console.WriteLine("payloadSchema=" + data.SchemaVersion + " lastVersion=" + data.LastGameVersion + " createdVersion=" + data.GameVersionCreated);
                Console.WriteLine("resourceKeys=" + data.Resources.Count + " quests=" + data.Quests.Count + " skills=" + data.SkillData.Count + " disciples=" + data.Disciples.Count);
                File.WriteAllText(args[1], JsonConvert.SerializeObject(data, Formatting.Indented));
                if (args.Length > 2) File.WriteAllBytes(args[2], SerializationUtility.SerializeValue(data, DataFormat.Binary));
                return 0;
            }
        }
        catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
