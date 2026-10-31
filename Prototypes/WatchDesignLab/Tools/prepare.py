#!/usr/bin/env python3
"""Reproduce lab art from exact Unity sprite fileIDs. Never changes importers."""
from pathlib import Path
import re,json,hashlib,shutil,subprocess,struct,sys
root=Path(__file__).resolve().parents[3]; lab=root/'Prototypes/WatchDesignLab'; catalog=lab/'Assets.xcassets'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,obj):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(obj,indent=2)+'\n')
write(catalog/'Contents.json',{'info':{'author':'xcode','version':1}})
guidpaths={}
for p in (root/'Assets/Art').rglob('*.png.meta'):
    m=re.search(r'^guid: (\w+)',p.read_text(),re.M)
    if m:guidpaths[m[1]]=p.with_suffix('')
cache={}; jobs=[]; provenance=[]
def layer(guid,fid):
    p=guidpaths[guid]
    if guid not in cache:
        sprites={}
        for block in re.split(r'    - serializedVersion: \d+\n',p.with_suffix('.png.meta').read_text())[1:]:
            mid=re.search(r'      internalID: (-?\d+)',block)
            rect=re.search(r'      rect:\n        serializedVersion: \d+\n        x: (\d+)\n        y: (\d+)\n        width: (\d+)\n        height: (\d+)',block)
            if mid and rect:sprites[mid[1]]=list(map(int,rect.groups()))
        cache[guid]=sprites
    return {'path':str(p),'rect':cache[guid][str(fid)],'fileID':str(fid),'guid':guid,'sha256':sha(p)}
def add(name,layers,pose=None):
    folder=catalog/(name+'.imageset');folder.mkdir(exist_ok=True)
    output=folder/(name+'.png')
    write(folder/'Contents.json',{'images':[{'filename':output.name,'idiom':'universal'}],'info':{'author':'xcode','version':1}})
    jobs.append({'output':str(output),'layers':[{k:l[k] for k in ['path','rect']} for l in layers]})
    provenance.append({'asset':name,'pose':pose,'layers':[{**l,'path':str(Path(l['path']).relative_to(root))} for l in layers]})
for name in ['EOVHero','EOVTask','EOVKill','EOVChest','EOVCarl','EOVCarlSwing','EOVResource']:
    p=root/'Assets/Editor/iOSLiveActivity/Art'/(name+'.png');folder=catalog/(name+'.imageset');folder.mkdir(exist_ok=True);shutil.copy2(p,folder/p.name)
    write(folder/'Contents.json',{'images':[{'filename':p.name,'idiom':'universal'}],'info':{'author':'xcode','version':1}})
    provenance.append({'asset':name,'source':str(p.relative_to(root)),'sha256':sha(p)})
parts=['fb371b21c67a2edb0d83b3260e2f784b','b202cad9312eae6f9e1d5ba265068595','0b53dbd7dbb93c685855ad2baf165954','7a59628089141039bdb3e500a8f8b37b','441f8073b294e0667d8d140f88135f1f','3be63e8bd6a3db850abf1e5406f3dd54']
frames=[re.findall(r'\{fileID: (-?\d+), guid: (\w+), type: 3\}',(root/'Assets/Resources/Characters/Parts'/(p+'.asset')).read_text().split('  frames:',1)[1]) for p in parts]
for pose,offset in [('Idle',0),('Run',24)]:
    for n in range(6):add(f'Hero{pose}_{n}',[layer(f[offset+n][1],f[offset+n][0]) for f in frames],f'{"Idle_Down" if pose=="Idle" else "Running_Right"} frame {n}, 8fps')
for n,fid in enumerate(['7436478308486094104','-1723881984','718037692','629843019']):add(f'CarlFly_{n}',[layer('76e09a37f606a514dbee18ee1fa1ebcc',fid)],f'FlyIn frame {n}, 12fps one-shot')
for name in ['Combat','Mining','Woodcutting','Fishing','Farming','Looting']:
    text=(root/'Assets/Scriptables/Skills'/(name+'.asset')).read_text();fid,guid=re.search(r'skillIcon: \{fileID: (-?\d+), guid: (\w+)',text).groups();add('Skill_'+name,[layer(guid,fid)])
for name,guid,fid in [('Farmlands','e36f7692c71d2a34383e21fe9d44dc80','-2997195532997922311'),('River','674345f33219f70418aca2a3db04ecbc','-4548796474974412338'),('Woods','b45d02bebb0bae34490e63de2e1440b9','621266246945384593'),('Mines','17ab04ce672d2cf4fbe3b9314f205693','-7112000108182893260'),('Haloween','3683944cef7e6bd4cae5ecd76295b35f','2029275289')]:add('Map_'+name,[layer(guid,fid)])
for name,path,rect in [
 ('TrailGrass','Assets/Art/Packs/Cute_Fantasy/Tiles/Grass/Grass_ForestGreen_Fill.png',None),
 ('TrailWater','Assets/Art/Packs/Cute_Fantasy/Tiles/Water/Water_Tile_2.png',[8,64,16,16]),
 ('TrailReeds','Assets/Art/Packs/Cute_Fantasy/Outdoor decoration/Outdoor_Decor_Animations/Grass_Animations/Grass_1_Anim.png',None)]:
    p=root/path;w,h=struct.unpack('>II',p.read_bytes()[16:24]);crop=rect or [0,h-16,16,16]
    add(name,[{'path':str(p),'rect':crop,'sha256':sha(p)}])
write(root/'Artifacts/WatchDesignLab/crop-jobs.json',jobs)
subprocess.run(['swift',str(lab/'Tools/crop.swift'),str(root/'Artifacts/WatchDesignLab/crop-jobs.json')],check=True)
write(lab/'SourceSprites.json',{'method':'Exact Unity sprite rects, bottom-left to top-left crop. Source-over RGBA in saved portrait order. No interpolation or source/importer edits.','assets':provenance})
print(f'{len(provenance)} source-traced assets')
