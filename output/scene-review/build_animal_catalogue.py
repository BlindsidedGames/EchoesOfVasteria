from pathlib import Path
from PIL import Image
import html,json
out=Path('output/scene-review/decor-proposal'); groups={}; inventory=[]
for line in Path('output/scene-review/animal-assets.txt').read_text().splitlines():
 p,n,rect=line.split('|'); inventory.append(dict(path=p,sprites=int(n)))
 if not p.endswith('.png'):continue
 if '/Animals/' in p:
  rel=p.split('/Animals/')[1]
  if rel.count('/')>1:continue
  group=rel.split('/')[0] if '/' in rel else 'Butterfly'
 elif '/Cat/' in p:group='Cats'
 else:group='Community duck'
 x,y,w,h=map(lambda a:int(float(a)),rect.split(','));im=Image.open(p).convert('RGBA');im=im.crop((x,im.height-y-h,x+w,im.height-y));
 if group=='Kapybara':im=im.crop((0,0,32,32))
 box=im.getbbox()
 if box:im=im.crop(box)
 if im.width>100 or im.height>100:im.thumbnail((64,64),Image.Resampling.NEAREST)
 scale=min(4,max(1,96//max(im.size)));im=im.resize((im.width*scale,im.height*scale),Image.Resampling.NEAREST)
 name=f'animal-{len(inventory):03}.png';im.save(out/name)
 groups.setdefault(group,[]).append((Path(p).stem,name,n,p))
plans=[('Duck','2','Wide lower river bend, one on each part of the bend','Short, separate water-only loops with drifting pauses. Never approach a bank closer than the sprite footprint.','Artwork sliced; swimming clips, decorative prefab and constrained patrol still needed.'),('Frog','2','Reed pockets on opposite banks of the lower valley','Occasional short hops between dry bank points, followed by long idle pauses.','Artwork sliced; verify hop frames and build controller/prefab.'),('Butterfly','3','Valley wildflowers and Eva’s garden','Small irregular loops above flowers; stagger pauses and speed.','Artwork sliced; flight playback and local flight route needed.'),('Bee','2','Berry bushes beside the farm','Short flower-to-flower visits, confined to the garden.','Flight sheet exists; decorative flight setup needed.'),('Mouse','2','Forest log pile and orchard edge','Brief scurries between cover, with eating/idle pauses.','Black mouse prefab, idle/walk/eat controller and wander logic already exist; check town navigation coverage.'),('Kapybara','1 optional','Quiet lower-river bank clearing','Resting at the water edge, with occasional look-around or dip animations; no walking animation assumed.','Raw strips currently imported as one sprite each; needs slicing, clips and a waterside idle sequence.'),('Swan','Keep existing','Original pond','Keep the existing sleeping swan as the pond landmark.','Sleeping swan prefab and water-sleep clips exist; no additional swans proposed.')]
body=['<h1>Animals for the town and valley</h1><p>Proposal only: no animals have been placed. These are actual asset previews, shown with nearest-neighbour scaling. Different sprite sheets contain different actions; preview frames are not a promise that their animation controllers are ready.</p><h2>Suggested first pass</h2><table><tr><th>Animal</th><th>Count</th><th>Where</th><th>Behaviour</th><th>Setup</th></tr>']
for row in plans:body.append('<tr>'+''.join('<td>'+html.escape(s)+'</td>' for s in row)+'</tr>')
body.append('</table><h2>Movement approach</h2><p>Use small authored habitat areas, with several valid waypoints and variable dwell times rather than synchronized circles. Land animals can reuse the existing wander/animation controller only after checking the navigation graph. Ducks need water-only waypoints and segment checks; the current land wander controller does not enforce water boundaries. Flying insects can use local airborne routes. Sort ground animals by their feet, use the pack’s native scale, and avoid paths, cliff faces and prop footprints.</p><p>I would keep cows, pigs, horses and sheep for a future paddock; geese for a deliberate farm feature; and camels, scarabs and vultures for a desert area. The cats are town characters/style alternatives, not extra forest wildlife. I found no rabbit, deer or fox sheets in the animal folders.</p><h2>Available artwork</h2>')
groups['Kapybara'].sort(key=lambda item: ('_Idle' not in item[0], item[0]))
for group,items in sorted(groups.items()):
 body.append(f'<h3>{html.escape(group)} · {len(items)} source sheets</h3><div class="cards">')
 for label,img,n,path in items:body.append(f'<figure><div class="sprite"><img src="{img}"></div><figcaption>{html.escape(label)}<small>{n} imported sprites</small></figcaption><details><summary>Source</summary>{html.escape(path)}</details></figure>')
 body.append('</div>')
body.append('<h2>Fish already in the river</h2><p>The existing Fish_Animated_Tile artwork and river fish remain. I would keep these occasional rather than increase density alongside the ducks.</p><p>Nested alternate copies and combat variants are retained in the inventory but omitted from the visual rows to avoid presenting duplicates as new animals.</p><a href="animal-inventory.json">Full inspected inventory</a>')
style='body{background:#202720;color:#e8ede6;font:16px system-ui;margin:32px;max-width:1500px}h1{font-size:30px}h2{margin-top:36px}p{max-width:1100px;line-height:1.55;color:#c6d0c2}table{border-collapse:collapse;width:100%}td,th{padding:13px;border-bottom:1px solid #52604f;text-align:left;vertical-align:top}th{background:#303d30}.cards{display:flex;flex-wrap:wrap;gap:12px}figure{background:#303b30;margin:0;width:175px;padding:12px}.sprite{height:110px;display:flex;align-items:center;justify-content:center}img{image-rendering:pixelated}small{display:block;color:#aebaab;margin-top:6px}details{font-size:11px;overflow-wrap:anywhere;margin-top:8px}a{color:#b4dfa6}'
(out/'animal-proposal.html').write_text('<!doctype html><meta charset="utf-8"><title>Animal artwork and placement proposal</title><style>'+style+'</style>'+''.join(body),encoding='utf-8')
(out/'animal-inventory.json').write_text(json.dumps(inventory,indent=2))
print({k:len(v) for k,v in groups.items()})
from PIL import ImageDraw
sheet=Image.new('RGB',(900,600),(35,45,35));draw=ImageDraw.Draw(sheet)
for i,(name,items) in enumerate(sorted(groups.items())):
 thumb=Image.open(out/items[0][1]);x=i%6*150;y=i//6*180
 sheet.paste(thumb,(x+20,y+15),thumb);draw.text((x+10,y+125),name,fill='white')
sheet.save('output/scene-review/animal-overview.png')

