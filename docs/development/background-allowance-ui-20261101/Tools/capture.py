from pathlib import Path
import subprocess, time

root = Path(__file__).resolve().parents[4]
out = root / 'docs/development/background-allowance-ui-20261101/captures'
app = root / 'Artifacts/WatchIntegration/DerivedData/Build/Products/Debug-watchsimulator/EchoesWatchPreview.app'
bundle = 'com.blindsidedgames.echoes.watch.integration.preview'
cases = [('dial', 'dial', 'allowance-live', 15), ('away', 'away', 'allowance-away', 85),
         ('legacy-away', 'away', 'away', 85), ('high-rate-away', 'away', 'allowance-high-rate', 2700)]
for size, device in [('42', '6EFA4422-289B-471C-A2B8-B2ADEDE14CDA'), ('46', '00B97A91-5573-4DFA-827B-0783D7096191')]:
    subprocess.run(['xcrun', 'simctl', 'install', device, str(app)], check=True)
    for name, screen, state, offset in cases:
        subprocess.run(['xcrun', 'simctl', 'launch', '--terminate-running-process', device, bundle,
                        '--screen=' + screen, '--state=' + state, '--offset=' + str(offset)], check=True, stdout=subprocess.DEVNULL)
        time.sleep(3 if name == 'dial' else 2)
        file = out / f'watch-{size}-{name}.png'
        subprocess.run(['xcrun', 'simctl', 'io', device, 'screenshot', str(file)], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        print(file.name, flush=True)
