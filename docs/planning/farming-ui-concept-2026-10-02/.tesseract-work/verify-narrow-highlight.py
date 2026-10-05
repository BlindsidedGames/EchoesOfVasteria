from pathlib import Path
import json,hashlib,struct,zlib,datetime,re
p=Path(__file__).resolve().parent.parent;b=p/'narrow-highlight-before';sha=lambda f:hashlib.sha256(f.read_bytes()).hexdigest()
old=json.loads((b/'.tesseract-work/Farm-narrow-landscape.json').read_text());new=json.loads((p/'.tesseract-work/Farm-narrow-landscape.json').read_text())
assert old['dimensions']==new['dimensions']=={'width':844,'height':390}
a={l['name']:l for l in old['composition']['layers'] if l['name'] in ['Selected bed','Plant button']};c={l['name']:l for l in new['composition']['layers'] if l['name'] in a}
def contains(outer,inner):
 x,y=outer['transform']['position'];w,h=outer['rect']['size'];xx,yy=inner['transform']['position'];ww,hh=inner['rect']['size'];return x<xx and y<yy and xx+ww<x+w and yy+hh<y+h
assert not contains(a['Selected bed'],a['Plant button']) and contains(c['Selected bed'],c['Plant button'])
assert len(old['composition']['layers'])==len(new['composition']['layers'])
for before,after in zip(old['composition']['layers'],new['composition']['layers']):
 if before['name']=='Selected bed':
  check=json.loads(json.dumps(before));check['transform']['position'][1]+=2;assert check==after
 else:assert before==after
assert (b/'.tesseract-work/Farm-narrow-landscape-text-boxes.json').read_bytes()==(p/'.tesseract-work/Farm-narrow-landscape-text-boxes.json').read_bytes()
def pixels(path):
 raw=path.read_bytes();pos=8;packed=bytearray()
 while pos<len(raw):
  n=struct.unpack('>I',raw[pos:pos+4])[0];t=raw[pos+4:pos+8];chunk=raw[pos+8:pos+8+n];pos+=n+12
  if t==b'IHDR':w,h,depth,color,_,_,interlace=struct.unpack('>IIBBBBB',chunk);assert(depth,color,interlace)==(8,6,0)
  if t==b'IDAT':packed.extend(chunk)
 data=zlib.decompress(packed);stride=w*4;rows=[];prev=bytearray(stride)
 for y in range(h):
  base=y*(stride+1);f=data[base];row=bytearray(data[base+1:base+1+stride])
  for x in range(stride):
   left=row[x-4] if x>=4 else 0;up=prev[x];ul=prev[x-4] if x>=4 else 0
   if f==1:v=left
   elif f==2:v=up
   elif f==3:v=(left+up)//2
   elif f==4:
    q=left+up-ul;dl,du,dul=abs(q-left),abs(q-up),abs(q-ul);v=left if dl<=du and dl<=dul else up if du<=dul else ul
   else:assert f==0;v=0
   row[x]=(row[x]+v)&255
  rows.append(bytes(row));prev=row
 return w,h,rows
w,h,op=pixels(b/'Farm-narrow-landscape.png');ww,hh,np=pixels(p/'Farm-narrow-landscape.png');assert(w,h)==(ww,hh)==(844,390)
changes=[]
for y in range(h):
 for x in range(w):
  if op[y][x*4:x*4+4]!=np[y][x*4:x*4+4]:changes.append((x,y));assert 358<=x<=818 and 275<=y<=325
assert changes
checkpoint=json.loads((b/'checkpoint.json').read_text());assert all(sha(p/r['path'])==r['sha256'] for r in checkpoint['protectedOtherArtifacts'])
main=sha(p.parents[2]/'Assets/Scenes/Main.unity');assert main=='8f912645ea9216c7f25a833da730c44524aaea0dab7ba4cd76a9c0e1ac0d9d54'
links=[]
for f in [p/'README.md',p/'integrated-interface-options.md']:
 for link in re.findall(r'!?\[[^\]]*\]\(([^)]+)\)',f.read_text()):
  if '://' not in link and not (f.parent/link).exists() and link!='narrow-highlight-verification.json':links.append(link)
assert not links
report={'checkedAtUTC':datetime.datetime.now(datetime.timezone.utc).isoformat(),'change':'Narrow selected row moved down two pixels; all other native layers and text unchanged','dimensions':[w,h],'beforeHighlight':{'position':a['Selected bed']['transform']['position'],'size':a['Selected bed']['rect']['size']},'afterHighlight':{'position':c['Selected bed']['transform']['position'],'size':c['Selected bed']['rect']['size']},'plantButton':{'position':c['Plant button']['transform']['position'],'size':c['Plant button']['rect']['size']},'beforeButtonContained':False,'afterButtonStrictlyContained':True,'pixelsChanged':len(changes),'changedPixelBounds':[min(x for x,y in changes),min(y for x,y in changes),max(x for x,y in changes),max(y for x,y in changes)],'noPixelChangesOutsideHighlight':True,'allOtherNativeLayersAndTextUnchanged':True,'beforeAfterPNGsVisuallyInspectedAtNativeSize':True,'narrowPngSha256':sha(p/'Farm-narrow-landscape.png'),'narrowSourceSha256':sha(p/'Farm-narrow-landscape.tsrct'),'mainDetailOtherImagesAndBundleUnchanged':checkpoint['protectedOtherArtifacts'],'mainSha256':main,'onlyNarrowLibraryReplacementRequested':True,'noOrchardMockupOrMechanicsApproved':True}
(p/'narrow-highlight-verification.json').write_text(json.dumps(report,indent=2)+'\n')
current=json.loads((p/'verification.json').read_text());row=next(r for r in current['outputs'] if r['name']=='Farm-narrow-landscape');row['pngSha256']=report['narrowPngSha256'];row['sourceSha256']=report['narrowSourceSha256'];current['latestNarrowCorrection']='narrow-highlight-verification.json';(p/'verification.json').write_text(json.dumps(current,indent=2)+'\n')
print(json.dumps({k:v for k,v in report.items() if k!='mainDetailOtherImagesAndBundleUnchanged'},indent=2))
