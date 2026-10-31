"""Capture production views through the isolated adapter, never a shipping cache."""
from pathlib import Path
import subprocess, time, sys

root = Path(__file__).resolve().parents[4]
out = root / 'docs/development/watch-integration-20261101/captures'
app = root / 'Artifacts/WatchIntegration/DerivedData/Build/Products/Debug-watchsimulator/EchoesWatchPreview.app'
bundle = 'com.blindsidedgames.echoes.watch.integration.preview'
devices = {'42': '6EFA4422-289B-471C-A2B8-B2ADEDE14CDA', '46': '00B97A91-5573-4DFA-827B-0783D7096191'}
cases = [
    ('01', 'automatic', 'hair', 'personalized-hair', False),
    ('03', 'health', 'hair', 'personalized-hair', False),
    ('01', 'automatic', 'offline', 'offline', False),
    ('05', 'automatic', 'away', 'automatic-away', False),
    ('05', 'automatic', 'stale', 'expired', False),
    ('05', 'automatic', 'restart', 'restart', False),
    ('01', 'automatic', 'dead', 'ended', False),
    ('01', 'automatic', 'empty', 'empty', False),
    ('07', 'skills', 'large', 'large-text', True),
]
for size, device in devices.items():
    if len(sys.argv) > 1 and size != sys.argv[1]:
        continue
    subprocess.run(['xcrun', 'simctl', 'install', device, str(app)], check=True)
    for ident, screen, state, name, large in cases:
        if len(sys.argv) > 2 and name not in sys.argv[2:]:
            continue
        args = ['--screen=' + screen, '--state=' + state]
        if large:
            args.append('--text=large')
        subprocess.run(['xcrun', 'simctl', 'launch', '--terminate-running-process', device, bundle, *args], check=True, stdout=subprocess.DEVNULL)
        time.sleep(2)
        file = out / f'{ident}-{size}-{name}.png'
        subprocess.run(['xcrun', 'simctl', 'io', device, 'screenshot', str(file)], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        print(file.name, flush=True)
