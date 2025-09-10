using Newtonsoft.Json;
using UnityEngine;

namespace VoxelBusters.CoreLibrary.Editor
{

    public partial class AssemblyDefinitionProxy
    {
        private struct AssemblyDefinitionData
        {
            #region Properties

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("references")]
            public string[] References { get; set; }

            [JsonProperty("optionalUnityReferences")]
            public string[] OptionalUnityReferences { get; set; }

            [JsonProperty("includePlatforms")]
            public string[] IncludePlatforms { get; set; }

            [JsonProperty("excludePlatforms")]
            public string[] ExcludePlatforms { get; set; }

            [JsonProperty("allowUnsafeCode")]
            public bool AllowUnsafeCode { get; set; }

            [JsonProperty("overrideReferences")]
            public bool OverrideReferences { get; set; }

            [JsonProperty("precompiledReferences")]
            public string[] PrecompiledReferences { get; set; }

            [JsonProperty("autoReferenced")]
            public bool AutoReferenced { get; set; }

            [JsonProperty("defineConstraints")]
            public string[] DefineConstraints { get; set; }

            #endregion

            #region Public methods

            public static AssemblyDefinitionData Load(string dataString)
            {
                return JsonConvert.DeserializeObject<AssemblyDefinitionData>(dataString);
            }

            public string ToJson()
            {
                return JsonConvert.SerializeObject(this, Formatting.Indented);
            }

            #endregion
        }

    }
}