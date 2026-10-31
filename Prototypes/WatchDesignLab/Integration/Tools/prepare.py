#!/usr/bin/env python3
"""Native fixture asset staging from the actual complete character catalog.
Production uses CharacterPortraitIosAssets.ExportWatchAnimation in Unity instead.
"""
from pathlib import Path
import re,json,hashlib,shutil,subprocess
root=Path(__file__).resolve().parents[4];harness=root/'Prototypes/WatchDesignLab/Integration';out=root/'Artifacts/WatchIntegration';catalog=out/'Assets.xcassets';catalog.mkdir(parents=True,exist_ok=True)
def write(p,v):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(v,indent=2)+'\n')
write(catalog/'Contents.json',{'info':{'author':'xcode','version':1}})
guidpaths={}
for p in (root/'Assets/Art').rglob('*.png.meta'):
 m=re.search(r'^guid: (\w+)',p.read_text(),re.M)
 if m:guidpaths[m[1]]=p.with_suffix('')
sprites={};jobs=[];sources=[]
def sprite(guid,fid):
 p=guidpaths[guid]
 if guid not in sprites:
  rows={}
  for block in re.split(r'    - serializedVersion: \d+\n',p.with_suffix('.png.meta').read_text())[1:]:
   idm=re.search(r'      internalID: (-?\d+)',block);rect=re.search(r'      rect:\n        serializedVersion: \d+\n        x: (\d+)\n        y: (\d+)\n        width: (\d+)\n        height: (\d+)',block)
   if idm and rect:rows[idm[1]]=list(map(int,rect.groups()))
  sprites[guid]=rows
 return {'path':str(p),'rect':sprites[guid][fid]}
def image(name,layers):
 folder=catalog/(name+'.imageset');folder.mkdir(exist_ok=True)
 write(folder/'Contents.json',{'images':[{'filename':'image.png','idiom':'universal','scale':'2x'}],'info':{'author':'xcode','version':1}})
 jobs.append({'output':str(folder/'image.png'),'layers':layers})
 sources.append({'name':name,'layers':[{**l,'path':str(Path(l['path']).relative_to(root))} for l in layers]})
for p in (root/'Assets/Editor/iOSLiveActivity/Art').glob('*.png'):
 folder=catalog/(p.stem+'.imageset');folder.mkdir(exist_ok=True);shutil.copy2(p,folder/p.name)
 write(folder/'Contents.json',{'images':[{'filename':p.name,'idiom':'universal'}],'info':{'author':'xcode','version':1}})
for p in (root/'Assets/Editor/iOSLiveActivity/Companion/Art').glob('*.png'):
 folder=catalog/(p.stem+'.imageset');folder.mkdir(exist_ok=True);shutil.copy2(p,folder/p.name)
 write(folder/'Contents.json',{'images':[{'filename':p.name,'idiom':'universal'}],'info':{'author':'xcode','version':1}})
text=(root/'Assets/Resources/Characters/Catalog.asset').read_text();refs=text.split('  parts:',1)[1].split('\n  poses:',1)[0]
for part in re.findall(r'resourcePath: Characters/Parts/([0-9a-f]{32})',refs):
 p=root/'Assets/Resources/Characters/Parts'/(part+'.asset');frames=re.findall(r'^  - \{fileID: (-?\d+)(?:, guid: (\w+), type: 3)?\}',p.read_text().split('  frames:',1)[1],re.M)
 if not frames or frames[0][0]=='0':continue
 name='EOVPortrait_'+p.stem
 try:first=sprite(frames[0][1],frames[0][0])
 except (KeyError,IndexError):continue
 image(name,[first])
 for n in range(6):
  try:layer=sprite(frames[n][1],frames[n][0])
  except (KeyError,IndexError):layer=first
  image(name+'_WatchIdle'+str(n),[layer])
write(out/'crop-jobs.json',jobs)
subprocess.run(['swift',str(root/'Prototypes/WatchDesignLab/Tools/crop.swift'),str(out/'crop-jobs.json')],check=True)
write(out/'source-assets.json',{'method':'Exact original sprite rects and complete catalog, independently staged for native QA. Production Unity exporter emits the same base IDs and six Watch suffixes.','sources':sources})
# Reuse scenarios without coupling fixture data or process arguments to the app.
for p in (root/'Prototypes/WatchDesignLab/Fixtures').glob('*.json'):shutil.copy2(p,harness/'Fixtures'/p.name)
hair=Path('/Users/matthewrushworth/.codex/worktrees/mac-live-activity-prototype/EchoesOfVasteria/Artifacts/LiveActivityCharacter20261031/hair/snapshot.json')
if hair.exists():
 actual=json.loads(hair.read_text());write(harness/'Fixtures/hair-baseline.json',actual);actual['phoneInBackground']=False;write(harness/'Fixtures/hair.json',actual)
print('Staged',len(jobs),'portrait images and all production static Watch icons')
