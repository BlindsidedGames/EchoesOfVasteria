#!/usr/bin/env python3
"""Record real native simulator rendering. No synthetic touch or game progress."""
from pathlib import Path
import subprocess,sys,time,signal
root=Path(__file__).resolve().parents[3];size,design,state,name=sys.argv[1:5];seconds=float(sys.argv[5]) if len(sys.argv)>5 else 6
ids={'42':'6EFA4422-289B-471C-A2B8-B2ADEDE14CDA','46':'00B97A91-5573-4DFA-827B-0783D7096191'}
out=root/'docs/development/watch-design-lab-20261031/clips'/(name+'.mp4');out.parent.mkdir(parents=True,exist_ok=True)
with (root/'Artifacts/WatchDesignLab'/(name+'.log')).open('w') as log:
 proc=subprocess.Popen(['xcrun','simctl','io',ids[size],'recordVideo','--codec=h264','--force',str(out)],stdout=log,stderr=subprocess.STDOUT)
 try:
  time.sleep(.5)
  if design!='current': subprocess.run(['xcrun','simctl','launch','--terminate-running-process',ids[size],'com.blindsidedgames.echoes.watch.designlab',f'--design={design}',f'--state={state}'],check=True,stdout=subprocess.DEVNULL)
  time.sleep(seconds)
 finally:
  proc.send_signal(signal.SIGINT);proc.wait(timeout=20)
print(out.name)
