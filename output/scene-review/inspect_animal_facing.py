from PIL import Image,ImageDraw
from pathlib import Path
rows=Path('output/scene-review/animal-facing.txt').read_text().splitlines()
out=Image.new('RGB',(900,((len(rows)+3)//4)*145),(55,65,55));d=ImageDraw.Draw(out)
for i,line in enumerate(rows):
 label,p,rect=line.split('|');x,y,w,h=map(lambda n:int(float(n)),rect.split(','));im=Image.open(p).convert('RGBA');im=im.crop((x,im.height-y-h,x+w,im.height-y));im=im.resize((w*3,h*3),Image.Resampling.NEAREST);ox=i%4*225;oy=i//4*145;out.paste(im,(ox,oy),im);d.text((ox+2,oy+105),label[:34],fill='white')
out.save('output/scene-review/animal-facing.png')
