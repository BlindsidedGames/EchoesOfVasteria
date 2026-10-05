"""Demonstrate approved omission with generated DTOs; no production/save writes.

Inputs are outputs of previous compatibility probes, not live save directories.
The experiment proves projection/serialization behavior, not platform upgrades.
"""
import argparse
import json
from pathlib import Path
import shutil
import subprocess

REPO = Path(__file__).resolve().parents[4]
EVIDENCE = Path(__file__).resolve().parent
UNITY = Path('/Applications/Unity/Hub/Editor/6000.5.5f1/Unity.app/Contents/Resources/Scripting')
MONO = UNITY / 'MonoBleedingEdge/bin/mono'
CSC = UNITY / 'DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll'


def compile_cs(folder, output, sources, extra=()):
    refs = ['mscorlib', 'System', 'System.Core', 'netstandard', 'Sirenix.Serialization',
            'Sirenix.Serialization.Config', 'Sirenix.OdinInspector.Attributes', 'UnityEngine.CoreModule']
    command = ['/usr/local/share/dotnet/dotnet', str(CSC), '-nologo', '-nostdlib',
               '-nowarn:0436', '-out:' + str(folder / output)]
    if output.endswith('.dll'):
        command.append('-target:library')
    command += ['-r:' + str(folder / (ref + '.dll')) for ref in refs + list(extra)]
    subprocess.run(command + list(map(str, sources)), check=True)


parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--current-managed', required=True, type=Path)
parser.add_argument('--typed-json', required=True, type=Path)
parser.add_argument('--lineage-dir', required=True, type=Path)
parser.add_argument('--out', required=True, type=Path)
args = parser.parse_args()
args.out.mkdir(parents=True, exist_ok=True)
managed = args.out / 'managed'
managed.mkdir(exist_ok=True)
for file in args.current_managed.glob('*.dll'):
    shutil.copy2(file, managed / file.name)
for file in (REPO / 'Assets/Plugins/Sirenix/Assemblies/NoEmitAndNoEditor').glob('*.dll'):
    shutil.copy2(file, managed / file.name)
original = (REPO / 'Assets/Scripts/Blindsided/SaveData/GameData.cs').read_text().replace(
    '[PreviouslySerializedAs("Milestones")]',
    '[UnityEngine.Serialization.FormerlySerializedAs("Milestones")]')
compatible = args.out / 'CompatibleGameData.cs'
compatible.write_text(original)
compile_cs(managed, 'TimelessEchoes.Runtime.dll', [compatible])
canonicalizer = args.out / 'Canonicalize.cs'
canonicalizer.write_text('''using System.IO; using Newtonsoft.Json; using Newtonsoft.Json.Linq;
using Blindsided.SaveData;
class Canonicalize { static void Main(string[] a) {
var root=JObject.Parse(File.ReadAllText(a[0]));
foreach(var s in ((JObject)root["SkillData"]??new JObject()).Properties()) {
var v=(JObject)s.Value; var m=v["Milestones"] as JArray;
if(m!=null && m.Count>0 && m[0].Type==JTokenType.String) v.Remove("Milestones"); }
var d=JsonConvert.DeserializeObject<GameData>(root.ToString(),new JsonSerializerSettings {
ObjectCreationHandling=ObjectCreationHandling.Replace});
File.WriteAllText(a[1],JsonConvert.SerializeObject(d,Formatting.Indented)); }}''')
compile_cs(managed, 'Canonicalize.exe', [canonicalizer], ['TimelessEchoes.Runtime', 'Newtonsoft.Json'])
inputs = [('typed-1.4.3', args.typed_json)]
inputs += [('string-' + p.stem, p) for p in args.lineage_dir.glob('*-string-era.json')]
inputs += [(p.stem, p) for p in sorted(args.lineage_dir.glob('es3-*.input.json'))]
canonical_paths = []
for name, input_path in inputs:
    output = args.out / (name + '-compatible.json')
    subprocess.run([str(MONO), str(managed / 'Canonicalize.exe'), str(input_path), str(output)], check=True)
    canonical_paths.append((name, output))

future = original.replace('public const int CurrentSchemaVersion = 3;',
                          'public const int CurrentSchemaVersion = 4;')
future = future.replace('''        [ShowInInspector] [HideReferenceObjectPicker] [TabGroup("GameDataTabs", "UpgradeSystem")]
        public Dictionary<string, int> UpgradeLevels = new();''', '')
future = future.replace('''        [TabGroup("GameDataTabs", "UpgradeSystem")]
        public bool StatUpgradesMigratedToGear;''', '')
future = future.replace('        [TabGroup("GameDataTabs", "Gear")] public bool DuckHelmetSanitized;', '')
future = future.replace('            public int TierIndex = -1;', '')
start = future.index('            // Preserve typed milestone records')
end = future.index('\n        [HideReferenceObjectPicker]\n        public class MilestoneProgressRecord', start)
future = future[:start] + '''            public HashSet<string> ActiveMilestoneIds = new();
        }
''' + future[end:]
target = args.out / 'FutureGameData.cs'
target.write_text(future)
compile_cs(managed, 'TimelessEchoes.Runtime.dll', [target])
compile_cs(managed, 'OmissionProbe.exe', [EVIDENCE / 'OmissionProbe.cs'], ['TimelessEchoes.Runtime', 'Newtonsoft.Json'])
results = []
for name, input_path in canonical_paths:
    destination = args.out / name
    result_process = subprocess.run([str(MONO), str(managed / 'OmissionProbe.exe'), str(input_path), str(destination)],
                   capture_output=True, text=True)
    if result_process.returncode:
        raise RuntimeError(name + ': ' + result_process.stderr)
    result = json.loads(Path(str(destination) + '.json').read_text())
    results.append({'fixture': name, **result})
(args.out / 'omission-results.json').write_text(json.dumps(results, indent=2))
print('Passed projections:', len(results), 'skills:', sum(r['skillCount'] for r in results),
      'typed choices checked:', sum(r['typedIdActivePairsEqual'] for r in results),
      'persisted active IDs:', sum(r['persistedActiveIds'] for r in results))
print('Private evidence:', args.out)
