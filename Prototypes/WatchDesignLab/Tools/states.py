#!/usr/bin/env python3
"""Additional native captures; UI interaction evidence is recorded separately via CUA."""
from pathlib import Path
import subprocess,time,json
root=Path(__file__).resolve().parents[3];out=root/'docs/development/watch-design-lab-20261031/captures';bundle='com.blindsidedgames.echoes.watch.designlab'
devices={'42':'6EFA4422-289B-471C-A2B8-B2ADEDE14CDA','46':'00B97A91-5573-4DFA-827B-0783D7096191'}
cases=[(d,'large-text',None,['--text=large']) for d in range(1,11)]
cases += [(d,'large-values','large-away' if d==5 else 'large-history' if d>=8 else 'large',[]) for d in [1,4,5,8,9,10]]
cases += [(d,'beyond-history','beyond-history',[]) for d in [1,2,10]]
cases += [(1,'empty','empty',[]),(1,'town','town',[]),(1,'offline','offline',[]),(2,'offline','offline',[]),(3,'away','away',[]),(4,'away','away',[]),(5,'restart','restart',[]),(5,'expired','stale',[]),(1,'no-reap','no-reap',[]),(2,'no-reap','no-reap',[])]
cases += [(d,'no-history','no-history',[]) for d in [8,9,10]]
for size,device in devices.items():
 for design,name,state,extra in cases:
  args=[f'--design={design}']+([f'--state={state}'] if state else [])+extra
  subprocess.run(['xcrun','simctl','launch','--terminate-running-process',device,bundle]+args,check=True,stdout=subprocess.DEVNULL)
  time.sleep(.65)
  filename=f'{design:02d}-{size}-{name}.png'
  subprocess.run(['xcrun','simctl','io',device,'screenshot',str(out/filename)],check=True,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
  print(filename,flush=True)
(root/'Artifacts/WatchDesignLab/state-cases.json').write_text(json.dumps(cases,indent=2)+'\n')
