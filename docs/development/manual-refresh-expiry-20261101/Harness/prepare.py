"""Stage source game art for the disposable host from the existing native catalog.

Run Prototypes/WatchDesignLab/Integration/Tools/prepare.py first if needed.
No production, old release or player files are written.
"""
from pathlib import Path
import json, shutil

folder = Path(__file__).resolve().parent
root = folder.parents[3]
source = root / 'Artifacts/WatchIntegration/Assets.xcassets'
snapshot = json.loads((folder / 'snapshot.json').read_text())
assets = folder / 'Assets.xcassets'
assets.mkdir(exist_ok=True)
(assets / 'Contents.json').write_text('{"info":{"author":"xcode","version":1}}')
names = ['EOVHero', 'EOVTask', 'EOVKill', 'EOVChest', 'EOVCarl', 'EOVCarlSwing'] + snapshot['portraitLayers']
for name in names:
    shutil.copytree(source / (name + '.imageset'), assets / (name + '.imageset'), dirs_exist_ok=True)
