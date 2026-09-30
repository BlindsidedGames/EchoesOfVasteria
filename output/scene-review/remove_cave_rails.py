from PIL import Image,ImageDraw
from pathlib import Path
import json
src=Path('Assets/Art/Packs/Cute_Fantasy/Tiles/Cliff/Stone_Cliff_3_Cave_Entrance.png');im=Image.open(src).convert('RGBA');out=im.copy();mask=set()
for y in range(16,32):
 for x in [*range(18,22),*range(26,30)]:mask.add((x,y))
for y in [18,19,20,21,26,27,28,29]:
 for x in range(16,32):mask.add((x,y))
for x,y in mask:out.putpixel((x,y),im.getpixel((15,y)))
dest=Path('Assets/Art/Environment/Caves');dest.mkdir(parents=True,exist_ok=True);p=dest/'Stone_Cliff_3_Cave_Entrance_No_Rails.png';out.save(p)
changed=[(x,y) for y in range(48) for x in range(48) if im.getpixel((x,y))!=out.getpixel((x,y))];assert all(p in mask for p in changed);assert im.getchannel('A').tobytes()==out.getchannel('A').tobytes()
preview=Image.new('RGB',(816,460),'#465244');d=ImageDraw.Draw(preview)
for i,(a,label) in enumerate([(im,'Original'),(out,'Rails removed')]):
 a=a.resize((384,384),Image.Resampling.NEAREST);preview.paste(a,(i*408,38),a);d.text((i*408+12,12),label,fill='white')
preview.save('output/scene-review/cave-entrance-pixel-edit.png');print(json.dumps({'size':out.size,'changedPixels':len(changed),'outsideMaskChanged':0,'alphaUnchanged':True,'asset':str(p)}))

