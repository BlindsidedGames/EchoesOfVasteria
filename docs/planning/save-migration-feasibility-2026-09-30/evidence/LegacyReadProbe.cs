// Exact current SaveManager reader with its existing root-override seam; CLI process only.
using System;
using System.IO;
using Blindsided.SaveData;
class LegacyReadProbe
{
    static int Main(string[] args)
    {
        UnityEngine.Debug.unityLogger.logEnabled=false;
        SaveManager.SetRootPathForTests(args[0]);
        var result=SaveManager.Instance.LoadDetailedAsync("Save1").GetAwaiter().GetResult();
        Console.WriteLine("status="+result.Status+" succeeded="+result.Succeeded+" verified="+result.IntegrityVerified);
        Console.WriteLine("diagnostic="+result.Diagnostic);
        if(result.Data!=null) Console.WriteLine("schema="+result.Data.SchemaVersion+" resources="+result.Data.Resources.Count);
        return 0;
    }
}
