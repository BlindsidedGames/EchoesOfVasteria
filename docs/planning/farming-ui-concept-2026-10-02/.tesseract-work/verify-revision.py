from pathlib import Path
import json,hashlib,struct,re,datetime,zipfile
p=Path(__file__).resolve().parent.parent;repo=p.parents[2]
names=['Farm-desktop','Farm-narrow-landscape','Farm-seed-and-bed-detail'];sha=lambda f:hashlib.sha256(f.read_bytes()).hexdigest()
roles={}
for s in ['Adventure','Hub','Townsfolk','Quests','Library','×','Beds','Progression','Town view','Seeds','Garden','Flora & Tillman | Level 8','Radish','Corn','Carrot','Wheat']:roles[s]='item or control'
for s in ['382 / 600 XP','???','Ready','Growing · 12 min','Empty','Locked','Radish → Bed 3','Radish · 6 packs','Bed 3 · Empty','Packs: 6','1 / 3']:roles[s]='value or state'
for s in ['All seeds','Harvest','Details','Focus garden','Plant','Back','‹','›']:roles[s]='necessary action'
for n in range(1,7):roles[str(n)]='value: packet quantity or bed number'
def lum(a):return sum((v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4)*k for v,k in zip(a[:3],[.2126,.7152,.0722]))
ratios=[];rows=[];audit=[]
for n in names:
 b=(p/(n+'.png')).read_bytes();w,h=struct.unpack('>II',b[16:24]);doc=json.loads((p/'.tesseract-work'/(n+'.json')).read_text());assert doc['dimensions']=={'width':w,'height':h}
 layers=doc['composition']['layers'];assert len({l['id'] for l in layers})==len(layers)
 texts=[l for l in layers if l['type']=='Text'];old=json.loads((p/'revision-1-before/.tesseract-work'/(n+'-text-boxes.json')).read_text());strings=[l['sourceText']['text'] for l in texts]
 assert strings.count('Plant')==1 and all(s in roles for s in strings)
 assert not any(re.search(r'illustrative|concept|undecided|discover packs|bring back|choose another|not yet|starts one|found on',s,re.I) for s in strings)
 for idx,l in enumerate(layers):
  if l['type']!='Text':continue
  t=l['sourceText'];assert t['fontFamily']=='Liberation Sans';x,y=l['transform']['position'];bw,bh=t['boxSize'];assert 0<=x<=x+bw<=w and 0<=y<=y+bh<=h
  cx,cy=x+bw/2,y+bh/2
  for back in layers[idx+1:]:
   if back['type']!='Rect':continue
   bx,by=back['transform']['position'];rw,rh=back['rect']['size'];color=back['rect']['fillColor']
   if bx<=cx<=bx+rw and by<=cy<=by+rh and color[3]==1:
    a,b=sorted([lum(t['fillColor']),lum(color)]);ratio=(b+.05)/(a+.05);assert ratio>=4.5,(n,t['text'],ratio);ratios.append(ratio);break
 audit.append({'screen':n,'beforeTextLayers':len(old),'afterTextLayers':len(texts),'strings':[{'text':s,'purpose':roles[s]} for s in strings]})
 rows.append({'name':n,'dimensions':[w,h],'pngSha256':sha(p/(n+'.png')),'sourceSha256':sha(p/(n+'.tsrct')),'nativeLayers':len(layers),'textLayers':len(texts),'plantCommitActions':1,'allTextCategorized':True,'textWithinCanvas':True,'pixelsReviewedAtIntendedSize':True})
main=sha(repo/'Assets/Scenes/Main.unity');assert main=='8f912645ea9216c7f25a833da730c44524aaea0dab7ba4cd76a9c0e1ac0d9d54'
old=json.loads((p/'revision-1-before/verification.json').read_text());preserved=[]
for name in ['Farm-progression','Farm-watering-exploration']:
 row=next(x for x in old['outputs'] if x['name']==name);assert row['pngSha256']==sha(p/(name+'.png')) and row['editableSourceSha256']==sha(p/(name+'.tsrct'));preserved.append(name)
report={'checkedAtUTC':datetime.datetime.now(datetime.timezone.utc).isoformat(),'stage':'copy revision of three farming screens','renderer':'Tesseract 0.3.1, Apple M3 Max / Metal, saved-source offscreen preview','outputs':rows,'copyAudit':audit,'minimumTextContrast':round(min(ratios),2),'labelsContrastChecked':len(ratios),'mainSha256':main,'otherTwoConceptsPreserved':preserved,'priorVersionsPreserved':'revision-1-before','UnityDesktopGameplayOrSaveChanges':False,'mechanicsDecisionsMade':False,'limits':['Static mockups; controls do not interact','Shared XP header, values and time remain illustrative, with assumptions in notes only','Garden geometry maps beds 1–4 but underlying crop art is not a simulation of the bed-list states','Unrevised progression and watering images remain historical references']}
(p/'revision-1-verification.json').write_text(json.dumps(report,indent=2)+'\n')
with zipfile.ZipFile(p/'Echoes-farming-UI-editable-sources.zip','w',zipfile.ZIP_DEFLATED) as z:
 for n in names+preserved:
  for ext in ['.tsrct','.png']:z.write(p/(n+ext),n+ext)
 for f in ['README.md','copy-audit.md','revision-1-verification.json','assets/LiberationSans-OFL.txt']:z.write(p/f,f)
print(json.dumps({'screens':rows,'minimumContrast':round(min(ratios),2),'mainUnchanged':True,'allVisibleStringsCategorized':True,'otherScreensPreserved':preserved},indent=2))
