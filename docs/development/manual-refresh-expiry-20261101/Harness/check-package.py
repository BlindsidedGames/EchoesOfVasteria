"""Inspect the generated PBX graph, not production source spelling.
Run PackageReadback first, then pass its output directory here.
"""
import json
from pathlib import Path
import subprocess
import sys

folder = Path(sys.argv[1])
def graph(mode):
    wire = subprocess.check_output(['plutil', '-convert', 'json', '-o', '-', str(folder / (mode + '.pbxproj'))])
    objects = json.loads(wire)['objects']
    result = {}
    for target in objects.values():
        if target.get('isa') != 'PBXNativeTarget':
            continue
        paths = []
        for phase in target['buildPhases']:
            section = objects[phase]
            if section['isa'] not in ['PBXSourcesBuildPhase', 'PBXFrameworksBuildPhase']:
                continue
            for entry in section['files']:
                paths.append(objects[objects[entry]['fileRef']].get('path', ''))
        result[target['name']] = paths
    return result

session = 'EchoesLiveActivity/Companion/EOVWatchPhoneSession.swift'
legacy = graph('legacy')
assert legacy['UnityFramework'].count(session) == 1 and session not in legacy['Unity-iPhone'], 'negative control did not seed old exported membership'
for mode in ['fresh', 'migrated']:
    value = graph(mode)
    assert value['Unity-iPhone'].count(session) == 1, 'phone session must compile once in host: ' + mode
    assert session not in value['UnityFramework'], 'legacy framework membership survived: ' + mode
    assert any(p.endswith('/WatchConnectivity.framework') for p in value['Unity-iPhone']), 'host lacks phone transport linkage: ' + mode
    assert 'EchoesLiveActivity/EOVLiveActivityBridge.swift' in value['Unity-iPhone'], 'session and activity owner must share host module'
    assert 'EchoesLiveActivity/EOVLiveActivity.mm' in value['UnityFramework'], 'real ObjC lookup caller moved out of framework'
    print('PASS ' + mode + ': host session/activity owner, host WC link, framework ObjC caller, no duplicate phone owner')
assert graph('fresh') == graph('migrated'), 'reprocessing must restore the same build graph without duplicate memberships'
print('PASS repeated reprocessing repairs real legacy membership and preserves generated build graph')
