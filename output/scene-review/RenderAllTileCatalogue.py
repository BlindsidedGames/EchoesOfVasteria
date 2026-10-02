"""Deterministic native-pixel catalogues, using inspected mappings, never generated art."""
import json,pathlib,math,re,collections
from PIL import Image,ImageDraw,ImageFont
ROOT=pathlib.Path(__file__).resolve().parents[2]
PLAN=json.loads((ROOT/'docs/TerrainTiles/AllPacks/tiles.json').read_text())
OUT=ROOT/'output/tile-catalogues';OUT.mkdir(parents=True,exist_ok=True)
def portable_font(size,bold=False):
    candidates=(['C:/Windows/Fonts/segoeuib.ttf','/System/Library/Fonts/Supplemental/Arial Bold.ttf','/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf'] if bold else ['C:/Windows/Fonts/consola.ttf','/System/Library/Fonts/Menlo.ttc','/System/Library/Fonts/Supplemental/Courier New.ttf','/usr/share/fonts/truetype/dejavu/DejaVuSansMono.ttf'])
    for candidate in candidates:
        if pathlib.Path(candidate).is_file():return ImageFont.truetype(candidate,size)
    return ImageFont.load_default(size=size)
fonts={n:portable_font(n) for n in [14,15,17,18,22]}
title=portable_font(30,True)
def wrap(s,n):
    # Fixed slices retain every underscore and character across lines.
    return '\n'.join(s[i:i+n] for i in range(0,len(s),n))
def crop(sheet,s):
    im=Image.open(ROOT/sheet['path']).convert('RGBA');r=s['rect'];x,y,w,h=map(int,[r['x'],r['y'],r['width'],r['height']]);out=im.crop((x,im.height-y-h,x+w,im.height-y));im.close();return out
def render_header(im,heading,sub):
    d=ImageDraw.Draw(im);d.text((20,15),heading,font=title,fill='#152827');d.text((20,58),sub,font=fonts[18],fill='#273b36')
    d.text((20,86),'N = up | E = right | S = down | W = left. Side means the named material; frame identity never changes playback order.',font=fonts[17],fill='#273b36')
    d.text((20,113),'Unrotated artwork. Gray = transparent pixels. Concatenate wrapped names exactly. ? = mapping contains contextual uncertainty.',font=fonts[17],fill='#273b36')
def panel(im,sheet,s,x,y,counter):
    d=ImageDraw.Draw(im);d.rounded_rectangle((x,y,x+645,y+342),8,fill='#ffffff',outline='#c8d0c7')
    d.text((x+12,y+9),wrap(f"#{counter:04d} OLD "+s['old_name'],58),font=fonts[17],fill='#3f4943',spacing=3)
    art=crop(sheet,s);factor=min(8,int(160/max(art.size)));assert factor>=2
    art=art.resize((art.width*factor,art.height*factor),Image.Resampling.NEAREST);im.paste(Image.new('RGB',(160,160),'#c6c6c6'),(x+12,y+65));im.paste(art,(x+12,y+65),art)
    d.text((x+185,y+62),wrap('NEW '+s['name'],40),font=fonts[18],fill='#102e2c',spacing=3)
    role=s['semantic_review'].get('role','Approved variant');d.text((x+185,y+178),wrap(str(role),46)[:210],font=fonts[15],fill='#32433c',spacing=2)
    note=s['semantic_review'].get('uncertainty',[])
    if note:d.text((x+185,y+246),'? See mapping for intent/material limits.',font=fonts[15],fill='#8b481d')
    d.text((x+12,y+243),f"{s['rect']['width']:g}x{s['rect']['height']:g}px @ {factor}x",font=fonts[15],fill='#32433c')
    d.text((x+12,y+270),'GUID '+s['guid']+' | local ID '+s['localId'],font=fonts[14],fill='#32433c')
    d.text((x+12,y+296),wrap(pathlib.PurePosixPath(sheet['path']).name,64),font=fonts[15],fill='#546159',spacing=2)
groups=collections.defaultdict(list)
counter=0
for sh in PLAN['sheets']:
    pack=sh['path'].split('/')[3] if sh['path'].startswith('Assets/Art/Packs/') else 'BetterRuleTiles_Samples'
    for s in sh['sprites']:counter+=1;groups[pack].append((sh,s,counter))
index=[]
for pack,all_rows in sorted(groups.items()):
    small=[r for r in all_rows if max(r[1]['rect']['width'],r[1]['rect']['height'])<=80]
    large=[r for r in all_rows if max(r[1]['rect']['width'],r[1]['rect']['height'])>80]
    pages=[]
    for page,start in enumerate(range(0,len(small),24),1):
        rows=small[start:start+24];im=Image.new('RGB',(2660,160+math.ceil(len(rows)/4)*355+42),'#f2f3ec')
        render_header(im,pack+f' - variants {start+1}-{start+len(rows)} / {len(small)}',f'{len(all_rows)} imported variants in this pack; larger assemblies have dedicated native 2x pages.')
        for i,(sh,s,num) in enumerate(rows):panel(im,sh,s,15+i%4*660,150+i//4*355,num)
        ImageDraw.Draw(im).text((20,im.height-28),'Private owner review | semantic mapping: docs/TerrainTiles/AllPacks/tiles.json | identities, rectangles, pivots and pixels preserved',font=fonts[15],fill='#425047')
        name=f'{pack}-{page:03d}.png';im.save(OUT/name);pages.append({'file':name,'kind':'variants','identities':[s['guid']+':'+s['localId'] for _,s,_ in rows]})
    for i,(sh,s,num) in enumerate(large,1):
        art=crop(sh,s);art=art.resize((art.width*2,art.height*2),Image.Resampling.NEAREST)
        width=max(1550,art.width+580);height=max(720,art.height+220);im=Image.new('RGB',(width,height),'#f2f3ec')
        render_header(im,pack+f' - enlarged assembly {i}/{len(large)}',f'Variant #{num:04d}: every native pixel enlarged exactly 2x; crop boundaries preserved.')
        im.paste(Image.new('RGB',art.size,'#c6c6c6'),(20,170));im.paste(art,(20,170),art)
        d=ImageDraw.Draw(im);x=art.width+45;d.text((x,170),'OLD\n'+wrap(s['old_name'],45),font=fonts[18],fill='#3f4943',spacing=3)
        d.text((x,245),'NEW\n'+wrap(s['name'],45),font=fonts[18],fill='#102e2c',spacing=3)
        d.text((x,380),wrap(str(s['semantic_review'].get('role','Assembly')),50),font=fonts[17],fill='#32433c',spacing=3)
        d.text((20,height-35),'GUID '+s['guid']+' | local ID '+s['localId']+' | native '+str(int(s['rect']['width']))+'x'+str(int(s['rect']['height']))+'px',font=fonts[15],fill='#32433c')
        name=f'{pack}-assembly-{i:03d}.png';im.save(OUT/name);pages.append({'file':name,'kind':'assembly','identities':[s['guid']+':'+s['localId']]})
    index.append({'pack':pack,'sources':len({r[0]['path'] for r in all_rows}),'sprites':len(all_rows),'pages':pages})
    print(pack,len(all_rows),'sprites',len(pages),'pages',flush=True)
(OUT/'index.json').write_text(json.dumps({'status':'final labelled PNGs; PDF/render review pending','total_sprites':counter,'packs':index},indent=2))
assert sum(len(page['identities']) for pack in index for page in pack['pages'])==4800
