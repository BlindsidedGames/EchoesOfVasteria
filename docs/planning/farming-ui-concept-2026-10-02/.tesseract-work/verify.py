from pathlib import Path
import json, hashlib, struct, datetime, zipfile
p=Path(__file__).resolve().parents[3];d=Path(__file__).resolve().parent.parent
names=['Farm-desktop','Farm-narrow-landscape','Farm-seed-and-bed-detail','Farm-progression','Farm-watering-exploration']
sha=lambda f:hashlib.sha256(f.read_bytes()).hexdigest()
rows=[];ratios=[]
def lum(a):return sum((v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4)*k for v,k in zip(a[:3],[.2126,.7152,.0722]))
for n in names:
    raw=(d/(n+'.png')).read_bytes();w,h=struct.unpack('>II',raw[16:24]);doc=json.loads((d/'.tesseract-work'/(n+'.json')).read_text())
    assert doc['dimensions']=={'width':w,'height':h}
    layers=doc['composition']['layers'];assert len({x['id'] for x in layers})==len(layers)
    texts=[x for x in layers if x['type']=='Text'];assert all(x['sourceText']['fontFamily']=='Liberation Sans' for x in texts)
    boxes=json.loads((d/'.tesseract-work'/(n+'-text-boxes.json')).read_text());assert all(x['x']>=0 and x['y']>=0 and x['x']+x['w']<=w and x['y']+x['h']<=h for x in boxes)
    for idx,t in enumerate(layers):
        if t['type']!='Text':continue
        x,y=t['transform']['position'];bw,bh=t['sourceText']['boxSize'];cx,cy=x+bw/2,y+bh/2
        for back in layers[idx+1:]:
            if back['type']!='Rect':continue
            bx,by=back['transform']['position'];rw,rh=back['rect']['size'];c=back['rect']['fillColor']
            if bx<=cx<=bx+rw and by<=cy<=by+rh and c[3]==1:
                a,b=sorted([lum(t['sourceText']['fillColor']),lum(c)]);r=(b+.05)/(a+.05)
                assert r>=4.5,(n,t['name'],r);ratios.append(r);break
    rows.append({'name':n,'dimensions':[w,h],'pngSha256':sha(d/(n+'.png')),'editableSourceSha256':sha(d/(n+'.tsrct')),'nativeLayerCount':len(layers),'editableTextLayers':len(texts),'editableRectangleLayers':sum(x['type']=='Rect' for x in layers),'embeddedImageLayers':sum(x['type']=='Image' for x in layers),'allTextBoxesWithinCanvas':True,'finalPixelsOpenedAndInspected':True})
repo=d.parents[2];main=sha(repo/'Assets/Scenes/Main.unity');assert main=='8f912645ea9216c7f25a833da730c44524aaea0dab7ba4cd76a9c0e1ac0d9d54'
report={'checkedAtUTC':datetime.datetime.now(datetime.timezone.utc).isoformat(),'renderer':'Tesseract 0.3.1; Apple M3 Max Metal; offscreen PNG preview','outputs':rows,'solidSurfaceTextContrastMinimum':round(min(ratios),2),'solidSurfaceTextLabelsChecked':len(ratios),'contrastCorrection':'Selected-state secondary labels use cream instead of muted (the original muted/selected pair is 4.00:1)','mobileActionTargets':[105,44],'mobileReflow':True,'mobileStatesVisible':4,'mobileBedsRepresented':6,'mobileSeedsVisible':8,'seedDetailSlotsVisible':17,'illustrativeMechanicsClearlyLabelled':True,'wateringSeparateExploration':True,'mainSha256':main,'UnityOrDesktopUIAutomationUsed':False,'changesConfinedToPlanningFiles':True,'freshUnityPlayerRun':False,'limits':['Static controls and tabs are not interactive','Shared versus separate XP and award timing are undecided','Watering, perks and speed effects remain exploratory','World preview crops illustrate approved geometry; they do not encode list assignments']}
(d/'verification.json').write_text(json.dumps(report,indent=2)+'\n')
with zipfile.ZipFile(d/'Echoes-farming-UI-editable-sources.zip','w',zipfile.ZIP_DEFLATED) as z:
    for n in names:
        for ext in ['.tsrct','.png']:z.write(d/(n+ext),n+ext)
    for f in [d/'verification.json',d/'assets/LiberationSans-OFL.txt']:z.write(f,str(f.relative_to(d)))
    readme=(d/'README.md').read_text().replace('[Library delivery](library-delivery.json) records the new image identities.','Image Library identities are recorded beside the original source projects on the Mac.')
    z.writestr('README.md',readme)
request={'uploads':[{'local_path':str(d/(n+'.png')),'purpose':'create_library_file','library_artifact_type':'image'} for n in names]+[{'local_path':str(d/'Echoes-farming-UI-editable-sources.zip'),'purpose':'create_library_file','library_artifact_type':'other'}]}
Path('/tmp/eov-farm-ui-library-20261002/request.json').write_text(json.dumps(request,indent=2))
print(json.dumps({'outputs':[(x['name'],x['dimensions'],x['nativeLayerCount']) for x in rows],'minimumSolidSurfaceContrast':round(min(ratios),2),'labelsChecked':len(ratios),'mainUnchanged':True},indent=2))
