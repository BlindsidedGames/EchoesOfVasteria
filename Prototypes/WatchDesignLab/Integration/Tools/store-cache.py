"""Stage a controlled cache only in the isolated real-store QA bundle.

No production process arguments, transport replacement or phone recomputation.
Usage: python3 .../store-cache.py 42 foreground|away|expired
"""
from pathlib import Path
import json, subprocess, sys, time

root = Path(__file__).resolve().parents[4]
size, mode = sys.argv[1:]
device = {'42': '6EFA4422-289B-471C-A2B8-B2ADEDE14CDA', '46': '00B97A91-5573-4DFA-827B-0783D7096191'}[size]
bundle = 'com.blindsidedgames.echoes.watch.integration.store'
app = root / 'Artifacts/WatchIntegration/DerivedData/Build/Products/Debug-watchsimulator/EchoesWatchStore.app'
subprocess.run(['xcrun', 'simctl', 'install', device, str(app)], check=True)
subprocess.run(['xcrun', 'simctl', 'terminate', device, bundle], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
container = Path(subprocess.check_output(['xcrun', 'simctl', 'get_app_container', device, bundle, 'data'], text=True).strip())
fixture = 'live' if mode == 'foreground' else 'away'
snapshot = json.loads((root / 'Prototypes/WatchDesignLab/Integration/Fixtures' / (fixture + '.json')).read_text())
elapsed = 15 if mode == 'foreground' else 85 if mode == 'away' else snapshot['timeline']['horizonSeconds'] + 60
shift = time.time() - elapsed - snapshot['generatedAt']
snapshot['generatedAt'] += shift
for key in ['timeline', 'forecast']:
    if snapshot.get(key):
        snapshot[key]['generatedAt'] += shift
        if 'forecastEnd' in snapshot[key]:
            snapshot[key]['forecastEnd'] += shift
cache = container / 'Library/Application Support/EOVWatchCompanion/latest-v1.json'
cache.parent.mkdir(parents=True, exist_ok=True)
cache.write_text(json.dumps(snapshot))
subprocess.run(['xcrun', 'simctl', 'launch', device, bundle], check=True)
time.sleep(2)
ident = '01' if mode == 'foreground' else '05'
file = root / f'docs/development/watch-integration-20261101/captures/{ident}-{size}-store-{mode}.png'
subprocess.run(['xcrun', 'simctl', 'io', device, 'screenshot', str(file)], check=True)
print(file.name)
