"""Read backed-up ES3/snapshots; run isolated serializer probes. Never launch Unity.

Input bytes and repository source are read-only. Copies/materializations/binaries
go only under --out. Summary omits player names and full inventories.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import shutil
import subprocess

REPO = Path(__file__).resolve().parents[4]
EVIDENCE = Path(__file__).resolve().parent
UNITY = Path('/Applications/Unity/Hub/Editor/6000.5.5f1/Unity.app/Contents/Resources/Scripting')
MONO = UNITY / 'MonoBleedingEdge/bin/mono'
CSC = UNITY / 'DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll'
DOTNET = '/usr/local/share/dotnet/dotnet'


def normalize_es3(text):
    # Easy Save emits unquoted integer TaskRecords keys. Quote only integer
    # property names outside string literals; preserve every value/escape.
    out, i, inside, escape, changed = [], 0, False, False, 0
    while i < len(text):
        c = text[i]
        out.append(c)
        i += 1
        if inside:
            if escape:
                escape = False
            elif c == '\\':
                escape = True
            elif c == '"':
                inside = False
        elif c == '"':
            inside = True
        elif c in '{,':
            match = re.match(r'(\s*)(-?\d+)(\s*):', text[i:])
            if match:
                out.append(match[1] + '"' + match[2] + '"' + match[3] + ':')
                i += match.end()
                changed += 1
    return json.loads(''.join(out)), changed


def compile_cs(folder, output, sources, extra=()):
    refs = ['mscorlib', 'System', 'System.Core', 'netstandard', 'Sirenix.Serialization',
            'Sirenix.Serialization.Config', 'Sirenix.OdinInspector.Attributes', 'UnityEngine.CoreModule']
    command = [DOTNET, str(CSC), '-nologo', '-nostdlib', '-nowarn:0436', '-out:' + str(folder / output)]
    if output.endswith('.dll'):
        command.append('-target:library')
    command += ['-r:' + str(folder / (ref + '.dll')) for ref in refs + list(extra)]
    subprocess.run(command + list(map(str, sources)), check=True, cwd=REPO)


def run(folder, executable, *args):
    return subprocess.run([str(MONO), str(folder / executable), *map(str, args)],
                          check=True, text=True, capture_output=True).stdout


parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--backup-root', required=True, type=Path)
parser.add_argument('--current-managed', required=True, type=Path)
parser.add_argument('--out', required=True, type=Path)
args = parser.parse_args()
args.out.mkdir(parents=True, exist_ok=True)
managed = args.out / 'managed'
managed.mkdir(exist_ok=True)
for file in args.current_managed.glob('*.dll'):
    shutil.copy2(file, managed / file.name)
for file in (REPO / 'Assets/Plugins/Sirenix/Assemblies/NoEmitAndNoEditor').glob('*.dll'):
    shutil.copy2(file, managed / file.name)
model = args.out / 'CurrentGameData.cs'
model.write_text((REPO / 'Assets/Scripts/Blindsided/SaveData/GameData.cs').read_text().replace(
    '[PreviouslySerializedAs("Milestones")]',
    '[UnityEngine.Serialization.FormerlySerializedAs("Milestones")]'))
compile_cs(managed, 'TimelessEchoes.Runtime.dll', [model])
compile_cs(managed, 'LegacySkillsProbe.exe', [EVIDENCE / 'LegacySkillsProbe.cs'],
           ['TimelessEchoes.Runtime', 'Newtonsoft.Json'])
rows, private_manifest = [], []
for index, fixture in enumerate(sorted(args.backup_root.rglob('*.es3')), 1):
    row = {'fixtureId': f'es3-{index:02}', 'bytes': fixture.stat().st_size}
    original = fixture.read_bytes()
    row['sha256'] = hashlib.sha256(original).hexdigest()
    private_manifest.append({'fixtureId': row['fixtureId'], 'relativePath': str(fixture.relative_to(args.backup_root))})
    try:
        document, changed = normalize_es3(original.decode('utf-8-sig'))
        # Route by embedded entry, never by renamed file or its host directory.
        row['entries'] = []
        for key, entry in document.items():
            if not isinstance(entry, dict) or not isinstance(entry.get('value'), dict):
                row['entries'].append({'rootKey': key, 'unsupportedShape': True})
                continue
            value = entry['value']
            destination = args.out / (row['fixtureId'] + '-' + re.sub(r'[^A-Za-z0-9]', '_', key))
            material = destination.with_suffix('.input.json')
            material.write_text(json.dumps(value))
            run(managed, 'LegacySkillsProbe.exe', material, destination)
            result = json.loads(destination.with_suffix('.json').read_text())
            row['entries'].append({
                'rootKey': key, 'serializedType': entry.get('__type'),
                'dateStarted': value.get('DateStarted'), 'dateQuit': value.get('DateQuitString'),
                'schemaField': value.get('SchemaVersion'), 'lastVersion': value.get('LastGameVersion'),
                'skills': {k: {'Level': v['Level'], 'CurrentXP': v['CurrentXP'],
                              'legacyMilestoneCount': len(v.get('Milestones') or [])}
                           for k, v in value.get('SkillData', {}).items()},
                'levelAndXpFloatBitsRoundtripEqual': result['skillLevelAndXpFloatBitsEqual'],
                'knownDictionariesRoundtripEqual': all(result[k + 'RoundtripEqual'] for k in
                    ['Resources', 'Quests', 'TaskRecords', 'UpgradeLevels', 'Disciples']),
                'legacyMilestoneStringsArchived': (
                    result['stringMilestoneArchive'] == {k: v.get('Milestones')
                        for k, v in value.get('SkillData', {}).items()}
                    and all(isinstance(v, str) for v in
                        sum([v or [] for v in result['stringMilestoneArchive'].values()], []))),
                'completedCropQuestIds': [k for k, v in value.get('Quests', {}).items()
                    if v.get('Completed') and (k.startswith('Unlock ') or k == 'Which Witch')],
                'unknownVersionNotInvented': result['lastVersion'] == value.get('LastGameVersion'),
            })
        row['quotedIntegerPropertyCount'] = changed
    except Exception as error:
        row['error'] = str(error)
    assert fixture.read_bytes() == original, 'Input changed'
    rows.append(row)
(args.out / 'private-fixture-paths.json').write_text(json.dumps(private_manifest, indent=2))
(args.out / 'es3-results.json').write_text(json.dumps(rows, indent=2))

# Historical string-era GameData avoids treating typed-decoder empty lists as
# evidence that an old payload never contained milestones.
historical = args.out / 'StringEraGameData.cs'
historical.write_text(subprocess.check_output(['git', 'show',
    '546dbc4b6^:Assets/Scripts/Blindsided/SaveData/GameData.cs'], cwd=REPO, text=True))
compile_cs(managed, 'TimelessEchoes.Runtime.dll', [historical])
compile_cs(managed, 'HistoricalPayloadProbe.exe', [EVIDENCE / 'PayloadProbe.cs'],
           ['TimelessEchoes.Runtime', 'Newtonsoft.Json'])
binary_rows = []
for relative in ['Saves/Save1/Archive/snapshot_migrated_20250910_062033.bin',
                 'Saves/Save3/Archive/snapshot_migrated_20251014_010756.bin']:
    source = args.backup_root / relative
    copy = args.out / (source.parent.parent.name + '-string-era.bin')
    original = source.read_bytes()
    copy.write_bytes(original)
    output = copy.with_suffix('.json')
    run(managed, 'HistoricalPayloadProbe.exe', copy, output)
    data = json.loads(output.read_text())
    binary_rows.append({'fixture': relative, 'sha256': hashlib.sha256(original).hexdigest(),
        'lastVersion': data.get('LastGameVersion'), 'skills': data['SkillData'],
        'totalStringMilestones': sum(len(s.get('Milestones') or []) for s in data['SkillData'].values())})
    assert source.read_bytes() == original
(args.out / 'string-era-binary-results.json').write_text(json.dumps(binary_rows, indent=2))
compile_cs(managed, 'TimelessEchoes.Runtime.dll', [model])
for row in binary_rows:
    slot = Path(row['fixture']).parts[1]
    output = args.out / (slot + '-string-era')
    run(managed, 'LegacySkillsProbe.exe', output.with_suffix('.json'), args.out / (slot + '-adapted'))
    check = json.loads((args.out / (slot + '-adapted.json')).read_text())
    row['adaptedLevelAndXpFloatBitsRoundtripEqual'] = check['skillLevelAndXpFloatBitsEqual']
    row['archivedMilestoneStringsExact'] = all(
        skill['Milestones'] == check['stringMilestoneArchive'][name]
        for name, skill in row['skills'].items())
(args.out / 'string-era-binary-results.json').write_text(json.dumps(binary_rows, indent=2))
print('ES3 files:', len(rows), 'errors:', sum('error' in row for row in rows))
print('String-era binaries:', [(r['lastVersion'], r['totalStringMilestones']) for r in binary_rows])
print('Private evidence:', args.out)
