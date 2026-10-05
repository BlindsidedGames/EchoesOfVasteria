"""Reproduce the bounded, non-Editor migration investigation on copied inputs.

All generated binaries, materialized saves and experimental source stay under --out.
The repository, deployed Player and input backup are read-only. No Unity process starts.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import struct
import subprocess
import tempfile

REPO = Path(__file__).resolve().parents[4]
EVIDENCE = Path(__file__).resolve().parent
UNITY = Path('/Applications/Unity/Hub/Editor/6000.5.5f1/Unity.app/Contents/Resources/Scripting')
MONO = UNITY / 'MonoBleedingEdge/bin/mono'
CSC = UNITY / 'DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll'
DOTNET = '/usr/local/share/dotnet/dotnet'
SAVE = REPO / 'Assets/Scripts/Blindsided/SaveData'


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def compile_cs(folder, output, sources, extra=(), define=False):
    refs = ['mscorlib', 'System', 'System.Core', 'netstandard', 'Sirenix.Serialization',
            'Sirenix.Serialization.Config', 'Sirenix.OdinInspector.Attributes', 'UnityEngine.CoreModule']
    command = [DOTNET, str(CSC), '-nologo', '-nostdlib', '-nowarn:0436', '-out:' + str(folder / output)]
    if output.endswith('.dll'):
        command.append('-target:library')
    if define:
        command.append('-define:UNITY_INCLUDE_TESTS')
    command += ['-r:' + str(folder / (ref + '.dll')) for ref in refs + list(extra)]
    subprocess.run(command + list(map(str, sources)), check=True, cwd=REPO)


def run(folder, executable, *args):
    result = subprocess.run([str(MONO), str(folder / executable), *map(str, args)],
                            check=True, text=True, capture_output=True)
    print(result.stdout, end='')
    return result.stdout


def envelope(original, payload_path, destination, schema=1):
    original = Path(original).read_bytes()
    size_position = 14 + struct.unpack_from('<H', original, 12)[0]
    hmac_size = struct.unpack_from('<H', original, size_position + 4)[0]
    header = bytearray(original[:size_position + 6 + hmac_size])
    payload = Path(payload_path).read_bytes()
    struct.pack_into('<i', header, 0, schema)
    struct.pack_into('<i', header, size_position, len(payload))
    Path(destination).write_bytes(header + payload)


parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--backup', required=True, type=Path)
parser.add_argument('--released-managed', required=True, type=Path)
parser.add_argument('--current-managed', required=True, type=Path)
parser.add_argument('--out', type=Path, default=None)
args = parser.parse_args()
out = args.out or Path(tempfile.mkdtemp(prefix='echoes-migration-repro-'))
out.mkdir(parents=True, exist_ok=True)
before = digest(args.backup)
fixture = out / 'copied-snapshot.bin'
shutil.copy2(args.backup, fixture)
folders = {key: out / key for key in ('released', 'current', 'runner')}
for key, source in [('released', args.released_managed), ('current', args.current_managed),
                    ('runner', args.released_managed)]:
    folders[key].mkdir(exist_ok=True)
    for file in source.glob('*.dll'):
        shutil.copy2(file, folders[key] / file.name)
    if key != 'released':
        for file in (REPO / 'Assets/Plugins/Sirenix/Assemblies/NoEmitAndNoEditor').glob('*.dll'):
            shutil.copy2(file, folders[key] / file.name)

compile_cs(folders['released'], 'Probe.exe', [EVIDENCE / 'PayloadProbe.cs'],
           ['Assembly-CSharp', 'Newtonsoft.Json'])
run(folders['released'], 'Probe.exe', fixture, out / 'released.json')
compile_cs(folders['current'], 'TimelessEchoes.Runtime.dll', [SAVE / 'GameData.cs'])
compile_cs(folders['current'], 'Probe.exe', [EVIDENCE / 'PayloadProbe.cs'],
           ['TimelessEchoes.Runtime', 'Newtonsoft.Json'])
run(folders['current'], 'Probe.exe', fixture, out / 'current-stock.json')

# Still smaller candidate: the runtime-supported Unity rename attribute.
unity_attribute = out / 'GameData-unity-attribute.cs'
unity_attribute.write_text((SAVE / 'GameData.cs').read_text().replace(
    '[PreviouslySerializedAs("Milestones")]',
    '[UnityEngine.Serialization.FormerlySerializedAs("Milestones")]'))
compile_cs(folders['current'], 'TimelessEchoes.Runtime.dll', [unity_attribute])
run(folders['current'], 'Probe.exe', fixture, out / 'unity-attribute-load.json')

# Disposable candidate: keep the historical typed field's serialized name.
model = (SAVE / 'GameData.cs').read_text()
a = model.index('            public List<MilestoneProgressRecord> Milestones\n')
b = model.index('            internal void NormalizeMilestones()', a)
model = (model[:a] + model[b:]).replace('[PreviouslySerializedAs("Milestones")]\n            ', '')
model = model.replace('MilestoneRecords', 'Milestones')
retained = out / 'GameData-retained-field.cs'
retained.write_text(model)
compile_cs(folders['current'], 'TimelessEchoes.Runtime.dll', [retained])
run(folders['current'], 'Probe.exe', fixture, out / 'retained-load.json', out / 'retained.payload')
envelope(fixture, out / 'retained.payload', out / 'retained-roundtrip.bin')
run(folders['current'], 'Probe.exe', out / 'retained-roundtrip.bin', out / 'retained-reload.json')
old = json.loads((out / 'released.json').read_text())
reloaded = json.loads((out / 'retained-reload.json').read_text())
assert old['SkillData'] == reloaded['SkillData'], 'Typed milestone/XP/level values changed'

config = json.loads((EVIDENCE / 'released-config.json').read_text())
overflow = SAVE / 'Migrations/Migration_CauldronOverflowRedistribution.cs'
profile = out / 'ReleasedProfileOverflow.cs'
profile.write_text(overflow.read_text()
                   .replace('ResourceCardMaximum = 500;', f"ResourceCardMaximum = {config['resourceThresholds'][-1]};")
                   .replace('BuffCardMaximum = 300;', f"BuffCardMaximum = {config['buffThresholds'][-1]};"))
common = [REPO / 'Assets/Scripts/Blindsided/Utilities/VersionUtil.cs', EVIDENCE / 'MigrationProbe.cs']
migrations = list((SAVE / 'Migrations').glob('*.cs'))
for name, model_source, overflow_source, input_json in [
    ('stock', SAVE / 'GameData.cs', overflow, out / 'current-stock.json'),
    ('combined', retained, profile, out / 'retained-load.json'),
    ('unity-attribute', unity_attribute, profile, out / 'unity-attribute-load.json')]:
    sources = [model_source, *common, *[p for p in migrations if p != overflow], overflow_source]
    compile_cs(folders['runner'], 'MigrationProbe.exe', sources, ['Assembly-CSharp', 'Newtonsoft.Json'])
    run(folders['runner'], 'MigrationProbe.exe', input_json, out / (name + '.json'), out / (name + '.payload'))
envelope(fixture, out / 'combined.payload', out / 'combined-roundtrip.bin', schema=3)
run(folders['current'], 'Probe.exe', out / 'combined-roundtrip.bin', out / 'combined-reload.json')
assert old['SkillData'] == json.loads((out / 'combined-reload.json').read_text())['SkillData']
compile_cs(folders['current'], 'TimelessEchoes.Runtime.dll', [unity_attribute])
envelope(fixture, out / 'unity-attribute.payload', out / 'unity-attribute-roundtrip.bin', schema=3)
run(folders['current'], 'Probe.exe', out / 'unity-attribute-roundtrip.bin', out / 'unity-attribute-reload.json')
alias_reload = json.loads((out / 'unity-attribute-reload.json').read_text())
for key, skill in old['SkillData'].items():
    assert all(value == alias_reload['SkillData'][key][field] for field, value in skill.items())
    assert skill['Milestones'] == alias_reload['SkillData'][key]['MilestoneRecords']
for key in ['Resources', 'EquipmentBySlot', 'Disciples']:
    assert old[key] == alias_reload[key]

# Exact reader source; compile only the real pure rescue/export helpers needed to link it.
helpers = (SAVE / 'SaveImportExport.cs').read_text()
helpers = helpers[:helpers.index('        public static string ExportCurrentSlot(')] + helpers[
    helpers.index('        internal static bool TryWriteRescuePayload('):]
helper_path = out / 'SaveImportExportPureHelpers.cs'
helper_path.write_text(helpers.replace('using Blindsided.SaveData.Migrations;', ''))
compile_cs(folders['current'], 'LegacyReadProbe.exe',
           [SAVE / 'GameData.cs', SAVE / 'SaveManager.cs', helper_path, EVIDENCE / 'LegacyReadProbe.cs'],
           ['System.IO.Compression', 'System.IO.Compression.FileSystem',
            'UnityEngine.JSONSerializeModule', 'UnityEngine.ScriptingModule'], define=True)
for case in ['primary', 'recovered', 'corrupt', 'future']:
    root = out / ('reader-' + case)
    slot = root / 'Saves/Save1'
    slot.mkdir(parents=True, exist_ok=True)
    data = bytearray(fixture.read_bytes())
    if case == 'future':
        struct.pack_into('<i', data, 0, 999)
    if case in ['corrupt', 'recovered']:
        data = data[:16]
    (slot / 'snapshot.bin').write_bytes(data)
    if case == 'recovered':
        shutil.copy2(fixture, slot / 'snapshot.prev1.bin')
    print(case, run(folders['current'], 'LegacyReadProbe.exe', root))
assert digest(args.backup) == before, 'Input backup changed'
print('Evidence directory:', out)
