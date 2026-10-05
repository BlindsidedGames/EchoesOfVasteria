// Runs the checked-out migration source on detached JSON materializations in a CLI process.
// PayloadProbe independently verifies actual historic Odin payload decoding.
using System;
using System.IO;
using System.Linq;
using Blindsided.SaveData;
using Blindsided.SaveData.Migrations;
using Newtonsoft.Json;
class MigrationProbe
{
    class FailureProbe : ISaveMigration
    {
        public int? TargetSchema => null;
        public string TargetVersion => "9999.0.0";
        public string Id => "InvestigationFailureProbe";
        public void Apply(GameData data) { data.Resources.Clear(); throw new Exception("Disposable injected failure"); }
    }
    static int Main(string[] args)
    {
        UnityEngine.Debug.unityLogger.logEnabled = false;
        var source = JsonConvert.DeserializeObject<GameData>(File.ReadAllText(args[0]),
            new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace });
        var before = JsonConvert.SerializeObject(source);
        var result = SaveMigrationRunner.TryMigrate(source, "1.4.3");
        Console.WriteLine("success="+result.Succeeded+" changed="+result.Changed+" applied="+string.Join(",",result.AppliedIds));
        if (!result.Succeeded) { Console.WriteLine(result.Error); return 1; }
        File.WriteAllText(args[1],JsonConvert.SerializeObject(result.Data,Formatting.Indented));
        if(args.Length>2) File.WriteAllBytes(args[2],Sirenix.Serialization.SerializationUtility.SerializeValue(
            result.Data,Sirenix.Serialization.DataFormat.Binary));
        var second = SaveMigrationRunner.TryMigrate(result.Data,"1.4.3");
        Console.WriteLine("repeatSuccess="+second.Succeeded+" repeatChanged="+second.Changed);
        Console.WriteLine("sourceUnchanged="+(before==JsonConvert.SerializeObject(source)));
        SaveMigrationRunner.Register(new FailureProbe());
        var failed = SaveMigrationRunner.TryMigrate(result.Data,"9999.0.0");
        Console.WriteLine("failureRejected="+!failed.Succeeded+" originalReturned="+ReferenceEquals(failed.Data,result.Data));
        Console.WriteLine("candidateSourceUnchanged="+(File.ReadAllText(args[1])==JsonConvert.SerializeObject(result.Data,Formatting.Indented)));
        return (!second.Succeeded || second.Changed || failed.Succeeded) ? 2 : 0;
    }
}
