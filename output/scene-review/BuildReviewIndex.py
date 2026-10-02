import json,pathlib
from PIL import Image,ImageDraw,ImageFont
r=pathlib.Path(__file__).resolve().parents[2];o=r/'output/tile-catalogues';idx=json.loads((o/'index.json').read_text())
def font(size):
 for candidate in ['C:/Windows/Fonts/consola.ttf','/System/Library/Fonts/Menlo.ttc','/usr/share/fonts/truetype/dejavu/DejaVuSansMono.ttf']:
  if pathlib.Path(candidate).is_file():return ImageFont.truetype(candidate,size)
 return ImageFont.load_default(size=size)
im=Image.new('RGB',(1500,1050),'#f2f3ec');d=ImageDraw.Draw(im)
d.text((45,35),'Echoes - all tilemap packs',font=font(38),fill='#173d35')
d.text((45,95),'4,800 variants | 312 sheets | 1,552 TileBase objects',font=font(24),fill='#173d35')
d.text((45,140),'Nine private Library PDFs; every variant shown once. Native pixel previews.',font=font(21),fill='#344b42')
for i,p in enumerate(idx['packs']):
 y=210+i*65;d.text((45,y),p['pack'],font=font(24),fill='#173d35');d.text((690,y),f"{p['sprites']:4} variants | {p['sources']:3} sheets | {len(p['pages'])+1:3} PDF pages",font=font(22),fill='#344b42')
d.text((45,835),'N up / E right / S down / W left in unrotated source art.',font=font(22),fill='#173d35')
d.text((45,875),'Check placed-cell rotation, connections and inner grass notches before selection.',font=font(21),fill='#344b42')
d.text((45,920),'47 pre-existing unresolved Tile objects retained; zero new missing references.',font=font(21),fill='#344b42')
d.text((45,965),'Exact duplicates retained. Weak near-lookalike groups are explicitly unconfirmed.',font=font(21),fill='#344b42');im.save(o/'AllPacks-review-index.png')
captures=r/'output/scene-review/path-stage-captures';im=Image.new('RGB',(2560,870),'#f2f3ec');d=ImageDraw.Draw(im)
d.text((35,20),'Logging clearing - actual isolated runtime, cumulative state 5 (Unlock Idle)',font=font(30),fill='#173d35')
for i,(f,label) in enumerate([('final-stage-5-clearing.png','BEFORE entrance repair: square unbordered mouth'),('review-final-stage-5-clearing.png','AFTER: grass borders curve into the trail mouth')]):
 d.text((35+i*1280,80),label,font=font(25),fill='#173d35');im.paste(Image.open(captures/f),(i*1280,125))
im.save(o/'Path-clearing-before-after.png');print('Created readable index and final runtime comparison')
