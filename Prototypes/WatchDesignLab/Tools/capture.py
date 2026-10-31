#!/usr/bin/env python3
"""Launch isolated fixture target and preserve original native simulator pixels."""
import subprocess,time,json,sys
from pathlib import Path
root=Path(__file__).resolve().parents[3];dest=root/'docs/development/watch-design-lab-20261031/captures';dest.mkdir(parents=True,exist_ok=True)
devices={'42':'6EFA4422-289B-471C-A2B8-B2ADEDE14CDA','46':'00B97A91-5573-4DFA-827B-0783D7096191'}
app=root/'Artifacts/WatchDesignLab/DerivedData/Build/Products/Debug-watchsimulator/EchoesWatchDesignLab.app';bundle='com.blindsidedgames.echoes.watch.designlab'
revision=sys.argv[1] if len(sys.argv)>1 else 'initial'
for size,device in devices.items():
 subprocess.run(['xcrun','simctl','install',device,str(app)],check=True)
 for design in range(1,11):
  subprocess.run(['xcrun','simctl','launch','--terminate-running-process',device,bundle,f'--design={design}'],check=True,stdout=subprocess.DEVNULL)
  time.sleep(1)
  out=dest/f'{design:02d}-{size}-{revision}.png'
  subprocess.run(['xcrun','simctl','io',device,'screenshot',str(out)],check=True,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
  print(out.name,flush=True)
