from pathlib import Path
from PIL import Image,ImageDraw
import json
base=Path('Assets/Art/Packs/Cute_Fantasy/Tiles/Cliff');mask={(x,y) for y in range(16,32) for x in [*range(18,22),*range(26,30)]}|{(x,y) for y in [18,19,20,21,26,27,28,29] for x in range(16,32)}
sheet=Image.new('RGB',(640,880),'#465244');d=ImageDraw.Draw(sheet);results=[]
for i in range(1,5):
 src=base/f'Stone_Cliff_{i}_Cave_Entrance.png';im=Image.open(src).convert('RGBA');assert im.size==(48,48)
 if i==3:out=Image.open('Assets/Art/Environment/Caves/Stone_Cliff_3_Cave_Entrance_No_Rails.png').convert('RGBA')
 else:
  out=im.copy()
  for x,y in mask:out.putpixel((x,y),im.getpixel((15,y)))
  dest=base/f'Stone_Cliff_{i}_Cave_Entrance_No_Rails.png';assert not dest.exists(),str(dest);out.save(dest)
 changes={(x,y) for y in range(48) for x in range(48) if im.getpixel((x,y))!=out.getpixel((x,y))};assert changes<=mask;assert im.getchannel('A').tobytes()==out.getchannel('A').tobytes();results.append({'variant':i,'changedPixels':len(changes),'outsideMaskChanged':0,'alphaUnchanged':True})
 for col,a in enumerate([im,out]):
  a=a.resize((192,192),Image.Resampling.NEAREST);sheet.paste(a,(col*320,(i-1)*220+24),a);d.text((col*320,(i-1)*220+5),f'{i} - '+('Original' if col==0 else 'No rails'),fill='white')
sheet.save('output/scene-review/cave-all-no-rails.png');print(json.dumps(results))
