from pathlib import Path
import json,hashlib,struct,re,datetime,zipfile
p=Path(__file__).resolve().parent.parent;repo=p.parents[2];sha=lambda f:hashlib.sha256(f.read_bytes()).hexdigest()
names=['Garden-Beds-desktop','Garden-Progression-desktop','Garden-Town-desktop']
roles={}
for s in ['Adventure','Hub','Townsfolk','Quests','Library','×','Flora & Tillman','Garden','Orchard · Locked','Beds','Progression','Town','Seeds','Access','Seed discoveries','Focus','Radish','Corn','Carrot','Wheat','Orchard']:roles[s]='item/control'
for s in ['???','4 / 6','4 / 17','Ready','Growing · 12 min','Empty','Locked','Available','1–2','3–4','5–6','Radish → Bed 3']:roles[s]='value/state'
for s in ['All seeds','Details','Harvest','Plant','Bed 1','Bed 2','Bed 3','Bed 4']:roles[s]='necessary action'
for i in range(1,7):roles[str(i)]='value: seed quantity or bed identity'
def lum(c):return sum((v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4)*k for v,k in zip(c[:3],[.2126,.7152,.0722]))
rows=[];contrast=[];copy=[]
for name in names:
 raw=(p/(name+'.png')).read_bytes();w,h=struct.unpack('>II',raw[16:24]);assert(w,h)==(1280,720)
 d=json.loads((p/'.tesseract-work'/(name+'.json')).read_text());assert d['dimensions']=={'width':w,'height':h};layers=d['composition']['layers'];assert len(layers)==len({l['id'] for l in layers})
 texts=[l for l in layers if l['type']=='Text'];strings=[l['sourceText']['text'] for l in texts];assert all(s in roles for s in strings)
 assert not any(re.search(r'illustrative|concept|undecided|level|\bXP\b|bring back|choose|not yet|starts one|found on',s,re.I) for s in strings)
 assert strings.count('Plant')==(1 if name=='Garden-Beds-desktop' else 0)
 for idx,l in enumerate(layers):
  if l['type']!='Text':continue
  t=l['sourceText'];assert t['fontFamily']=='Liberation Sans';x,y=l['transform']['position'];bw,bh=t['boxSize'];assert 0<=x<=x+bw<=w and 0<=y<=y+bh<=h
  cx,cy=x+bw/2,y+bh/2
  for back in layers[idx+1:]:
   if back['type']!='Rect':continue
   xx,yy=back['transform']['position'];rw,rh=back['rect']['size'];c=back['rect']['fillColor']
   if xx<=cx<=xx+rw and yy<=cy<=yy+rh and c[3]==1:
    a,b=sorted([lum(t['fillColor']),lum(c)]);ratio=(b+.05)/(a+.05);assert ratio>=4.5,(name,t['text'],ratio);contrast.append(ratio);break
 active=next(l for l in layers if l['name']==name.split('-')[1]+' button');assert active['rect']['fillColor']==[239/255,185/255,132/255,1]
 copy.append({'view':name,'strings':[{'text':s,'purpose':roles[s]} for s in strings]})
 rows.append({'name':name,'dimensions':[w,h],'pngSha256':sha(p/(name+'.png')),'nativeSourceSha256':sha(p/(name+'.tsrct')),'nativeLayers':len(layers),'textLayers':len(texts),'allTextWithinCanvas':True,'nativeSizePixelsVisuallyInspected':True})
checkpoint=json.loads((p/'desktop-workspace-before/checkpoint.json').read_text());preserved=[r for r in checkpoint['existingArtifactsRetainedInPlace'] if Path(r['path']).suffix in ['.png','.tsrct','.zip']];assert all(sha(p/r['path'])==r['sha256'] for r in preserved)
main=sha(repo/'Assets/Scenes/Main.unity');assert main==checkpoint['mainSha256']=='8f912645ea9216c7f25a833da730c44524aaea0dab7ba4cd76a9c0e1ac0d9d54'
sources=['Assets/Scripts/Farming/FarmState.cs','Assets/Scripts/Farming/FarmCommands.cs','Assets/Scripts/Farming/FarmService.cs','Assets/Scriptables/Skills/Farming.asset','Assets/Resources/Tasks/Farming/Radish.asset','Assets/Resources/Tasks/Farming/Corn.asset','docs/development/farming-radish-slice-2026-10-01/design-session-briefing.md','docs/planning/farming-expansion-2026-09-30/crop-asset-inventory.json','docs/planning/farming-expansion-2026-09-30/revision-4/seed-tree-art-inventory.json']
report={'checkedAtUTC':datetime.datetime.now(datetime.timezone.utc).isoformat(),'renderer':'Tesseract 0.3.1 offscreen Metal, saved native sources','outputs':rows,'minimumTextContrast':round(min(contrast),2),'textContrastLabelsChecked':len(contrast),'copyAudit':copy,'areaSwitch':'Garden active; Orchard locked for later access','threeSeparateViews':True,'numericNPCLevelsCurvesPerksOrUnlockPricesAdded':False,'TownImage':'Native 1x crop from actual V4 town render; existing world crop assignments do not simulate list state','oldImagesNativeSourcesAndBundlePreserved':preserved,'mainSha256':main,'baselineSourceHashes':[{'path':s,'sha256':sha(repo/s)} for s in sources],'onlyPlanningArtifactsChangedByThisTask':True,'UnityPlayerDesktopAutomationUsed':False,'mechanicsImplemented':False,'committedOrPushed':False,'limits':['Static concept only','Four open/six total beds and four discovered/seventeen entries are sample states','Paired bed milestones remain provisional visual staging, not approved unlock requirements','Orchard unlocked content/input/fruit loop depends on unsettled mechanics','Twins level/XP benefits and watering are undecided']}
(p/'workspace-verification.json').write_text(json.dumps(report,indent=2)+'\n')
with zipfile.ZipFile(p/'Echoes-Garden-Orchard-workspace-sources.zip','w',zipfile.ZIP_DEFLATED) as z:
 for name in names:
  for ext in ['.png','.tsrct']:z.write(p/(name+ext),name+ext)
 for s in ['workspace-notes.md','workspace-baseline.md','workspace-verification.json','assets/LiberationSans-OFL.txt']:z.write(p/s,s)
print(json.dumps({'views':rows,'minimumContrast':round(min(contrast),2),'stringsChecked':sum(len(x['strings']) for x in copy),'preservedEarlierArtifacts':len(preserved),'mainUnchanged':True},indent=2))
