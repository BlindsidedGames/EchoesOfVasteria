from pathlib import Path
import json, subprocess, struct, hashlib

HERE = Path(__file__).resolve().parent.parent
CLI = '/tmp/eov-farm-ui-tesseract-20261002/install/bin/tsrct'
FONT = HERE.parents[2] / 'Assets/TextMesh Pro/Fonts/LiberationSans.ttf'
COLORS = {'surface':'292426','text':'F5E4D6','muted':'C8AEA1','control':'3C3032','line':'74514A','accent':'EFB984','on':'302126','selected':'674635','field':'382E31','green':'94CB7D','shade':'191214'}
def rgba(c):
    c=COLORS.get(c,c); return [int(c[i:i+2],16)/255 for i in (0,2,4)]+[1]
def cli(*args):
    r=subprocess.run([CLI,*args],cwd=HERE,capture_output=True,text=True)
    if r.returncode: raise RuntimeError(r.stderr+r.stdout)
    return r.stdout
def size(asset):
    b=(HERE/'assets'/f'{asset}.png').read_bytes();return struct.unpack('>II',b[16:24])
def asset_id(asset):
    return asset+'-'+hashlib.sha256((HERE/'assets'/f'{asset}.png').read_bytes()).hexdigest()[:12]

class Board:
    def __init__(self,name,w=1280,h=720):
        self.name=name;self.w=w;self.h=h;self.layers=[];self.used=set();self.boxes=[]
    def base(self,name,x,y,t):
        return {'id':len(self.layers)+1,'name':name,'type':t,'blendMode':'normal','activeRange':{'start':0,'duration':3000},'transform':{'anchorPoint':[0,0],'position':[x,y],'scale':[100,100],'rotation':0,'opacity':100}}
    def rect(self,x,y,w,h,c='control',stroke=None,name='Panel'):
        l=self.base(name,x,y,'Rect');l['rect']={'size':[w,h],'fillColor':rgba(c)}
        if stroke:l['rect'].update(strokeColor=rgba(stroke),strokeEnabled=True,strokeWidth=1)
        self.layers.append(l)
    def text(self,s,x,y,w,h=24,fs=16,c='text',align='left',name=None):
        l=self.base(name or s,x,y,'Text');l['sourceText']={'text':s,'fontFamily':'Liberation Sans','fontStyle':'Regular','fontSize':fs,'fillColor':rgba(c),'strokeWidth':0,'justification':align,'boxText':True,'boxPosition':[0,0],'boxSize':[w,h],'verticalAlign':'center','leading':fs*1.22};self.layers.append(l);self.boxes.append({'text':s,'x':x,'y':y,'w':w,'h':h,'fontSize':fs})
    def img(self,a,x,y,w=None,h=None):
        sw,sh=size(a);w=w or sw;h=h or sh;l=self.base(a,x,y,'Image');l['source']={'assetId':asset_id(a),'fit':'contain'};l['transform']['scale']=[w/sw*100,h/sh*100];self.layers.append(l);self.used.add(a)
    def button(self,s,x,y,w,h=38,primary=False,fs=16):
        self.rect(x,y,w,h,'accent' if primary else 'control','line',s+' button');self.text(s,x+8,y,w-16,h,fs,'on' if primary else 'text','center')
    def track(self,x,y,w,f,h=4):
        self.rect(x,y,w,h,'line',name='XP/progress track');self.rect(x,y,w*f,h,'accent',name='XP/progress fill')
    def chrome(self,narrow=False):
        self.img('town',0,0,self.w,self.h);l=self.base('Dim town backdrop',0,0,'Rect');l['rect']={'size':[self.w,self.h],'fillColor':rgba('shade')[:-1]+[.72]};self.layers.append(l)
        if narrow:
            self.rect(12,10,self.w-24,self.h-20,'surface','line');self.button('Townsfolk',24,20,112,32,False,14);self.text('Garden',150,20,160,32,20);self.button('×',self.w-54,20,30,32,False,20)
        else:
            for s,x,w in [('Adventure',28,104),('Hub',140,56),('Townsfolk',204,110),('Quests',322,82),('Library',412,86)]:self.button(s,x,32,w,38,s=='Townsfolk',16)
            self.button('×',1212,32,40,38,False,24);self.rect(28,82,1224,610,'surface','line')
    def twins(self,narrow=False):
        if narrow:
            self.img('Flora',24,62,32,38);self.img('Tillman',60,62,32,38);self.text('Flora & Tillman | Level 8',105,60,340,26,18);self.track(460,79,322,.64);self.text('382 / 600 XP',590,87,192,20,13,'muted','right')
        else:
            self.img('Flora',49,104,32,38);self.img('Tillman',85,104,32,38);self.text('Flora & Tillman | Level 8',136,100,480,28,20);self.text('382 / 600 XP',984,100,244,28,14,'muted','right');self.track(136,140,1092,.64)
    def seed_grid(self,x,y,cols=4,rows=3,tw=80,th=76,selected='Radish',small=False,all_seeds=False):
        known=['Radish','Corn','Carrot','Wheat'];counts=[6,3,2,1]
        for i in range(17 if all_seeds else cols*rows):
            a=known[i] if i<4 else 'Unknown';label=a if i<4 else '???';xx=x+(i%cols)*tw;yy=y+(i//cols)*th
            self.rect(xx,yy,tw-5,th-5,'selected' if a==selected else 'field','accent' if a==selected else 'line',label+' seed slot')
            icon=32 if small else 48;self.img('SeedPack_'+a+('-2x' if small else '-3x'),xx+(tw-5-icon)/2,yy+4,icon,icon)
            self.text(label,xx+2,yy+icon+5,tw-9,16,13 if small else 14,'text','center')
            if i<4:self.text(str(counts[i]),xx+2,yy+icon+21,tw-9,14,13,'text' if a==selected else 'muted','center')
    def bed(self,n,s,status,action,y,selected=False,narrow=False):
        x=365 if narrow else 410;w=453 if narrow else 453;h=46 if narrow else 57
        if selected:self.rect(x-6,y-2,w+5,h,'selected','accent','Selected bed')
        self.text(str(n),x,y+6,26,26,16,'text' if selected else 'muted','center')
        if s not in ['Empty','Locked']:self.img('SeedPack_'+s+'-2x',x+34,y+6,32,32)
        else:self.rect(x+41,y+12,18,18,'field','line','Empty bed marker')
        self.text(s,x+78,y+2,220,24,16)
        if status:self.text(status,x+78,y+24,220,20,13,'green' if status=='Ready' else 'text' if selected else 'muted')
        if action:self.button(action,x+335,y+(1 if narrow else 5),105,44 if narrow else 36,action in ['Harvest','Plant'],15)
        if not selected:self.rect(x,y+h-2,w,1,'line',name='Bed row divider')
    def footer(self,s):
        self.rect(48,641,1184,1,'line');self.text(s,48,651,1184,26,13,'muted')
    def save(self):
        project=HERE/f'{self.name}.tsrct'
        if not project.exists():cli('project','create','--project',str(project))
        cli('project','import-font','--project',str(project),'--file',str(FONT))
        for a in sorted(self.used):
            try:cli('project','import-asset','--project',str(project),'--file',str(HERE/'assets'/f'{a}.png'),'--asset-id',asset_id(a),'--kind','image')
            except RuntimeError as e:
                if 'asset ID already exists; choose a new ID' not in str(e):raise
        layout=HERE/'.tesseract-work'/f'{self.name}.json';cli('project','checkout','--project',str(project),'--output',str(layout));doc=json.loads(layout.read_text());doc['dimensions']={'width':self.w,'height':self.h};doc['duration']=3;doc['composition']['name']=self.name;doc['composition']['layers']=list(reversed(self.layers));layout.write_text(json.dumps(doc,indent=2));cli('project','commit','--project',str(project),'--file',str(layout));print(cli('preview','--project',str(project),'--time','1','--output',str(HERE/f'{self.name}.png')).strip());(HERE/'.tesseract-work'/f'{self.name}-text-boxes.json').write_text(json.dumps(self.boxes,indent=2))

def desktop():
    b=Board('Farm-desktop');b.chrome();b.twins();b.text('Seeds',48,173,320,28,20);b.seed_grid(48,214,tw=81,th=91);b.button('All seeds',48,505,320,36,False,15)
    b.text('Beds',410,173,290,28,20)
    for args in [(1,'Carrot','Ready','Harvest'),(2,'Corn','Growing · 12 min','Details'),(3,'Empty','',None),(4,'Wheat','Ready','Harvest'),(5,'Locked','', 'Details'),(6,'Locked','', 'Details')]:b.bed(*args,214+(args[0]-1)*60,selected=args[0]==3)
    b.text('Garden',900,173,320,28,20);b.img('garden',900,214,264,302)
    for n,x,y in [(1,945,281),(2,1022,281),(3,945,427),(4,1022,427)]:
        b.rect(x,y,22,24,'surface','line','World bed '+str(n));b.text(str(n),x,y,22,24,16,'text','center')
    b.button('Focus garden',900,535,320,38);b.text('Radish → Bed 3',410,603,260,28,18);b.button('Plant',686,599,190,38,True);return b

def mobile():
    b=Board('Farm-narrow-landscape',844,390);b.chrome(True);b.twins(True)
    b.button('Beds',24,112,106,34,True,15);b.button('Progression',138,112,124,34,False,15);b.button('Town view',270,112,100,34,False,15)
    b.text('Seeds',24,157,320,24,18);b.seed_grid(24,186,cols=4,rows=2,tw=79,th=73,small=True);b.button('‹',24,337,34,30,False,20);b.text('1 / 3',66,337,226,30,14,'muted','center');b.button('›',302,337,34,30,False,20);b.text('Beds',365,157,284,24,18)
    for args in [(1,'Carrot','Ready','Harvest'),(2,'Corn','Growing · 12 min','Details'),(3,'Empty','Radish · 6 packs','Plant'),(4,'Wheat','Ready','Harvest')]:b.bed(*args,186+(args[0]-1)*46,selected=args[0]==3,narrow=True)
    b.rect(822,187,3,183,'line');b.rect(822,187,3,118,'accent');return b

def detail():
    b=Board('Farm-seed-and-bed-detail');b.chrome();b.twins();b.text('Seeds',48,173,650,30,22);b.seed_grid(48,214,cols=6,rows=3,tw=104,th=102,all_seeds=True)
    b.rect(719,173,1,441,'line');b.text('Bed 3 · Empty',750,173,430,30,22);b.img('SeedPack_Radish-3x',750,234,96,96);b.text('Radish',870,239,320,28,20);b.text('Packs: 6',870,274,320,24,16,'muted');b.button('Back',750,570,140,44);b.button('Plant',908,570,292,44,True);return b

def watering():
    b=Board('Farm-watering-exploration');b.chrome();b.twins();b.text('Watering · exploratory alternative',48,184,920,30,22);b.text('A separate concept for discussion; the main screens do not require watering.',48,220,1152,26,16,'muted')
    for i,(title,sub) in enumerate([('1 · Sown','First watering'),('2 · Growing','Second watering'),('3 · Mature','Final watering')]):
        x=48+i*249;b.rect(x,270,227,235,'selected' if i==1 else 'field','accent' if i==1 else 'line');b.text(title,x+14,284,199,28,20);sw,sh=size(f'Carrot-stage{i+1}');b.img(f'Carrot-stage{i+1}',x+(227-sw)/2,406-sh,sw,sh);b.text(sub,x+14,429,199,30,16,'text' if i==1 else 'muted','center');b.text('Done' if i==0 else 'Current' if i==1 else 'Next',x+14,465,199,25,15,'green' if i==0 else 'text','center')
    b.text('Bed 2 · Carrot',824,278,360,32,22);b.text('Illustrative stage 2 of 3',824,316,360,26,16,'muted');b.button('Water this stage',824,369,376,44,True);b.text('Possible progression perk',824,447,376,28,18);b.button('Auto-watering · undecided',824,483,376,42)
    b.text('Questions this variant raises',48,538,1130,28,20);b.text('Should watering be optional, required or a speed bonus?\nHow would auto-watering unlock, and when would the twins gain XP?',48,575,1140,55,18,'muted');b.footer('Exploratory only. Stage count, watering timing, auto-watering perks, speed effects and XP rules have no approved values.');return b

def progression():
    b=Board('Farm-progression');b.chrome();b.twins();b.text('Growing the garden',48,183,730,30,22);b.text('Progression preview · materials and unlock rules remain to be chosen',48,218,1148,24,16,'muted')
    for i,(name,subtitle,art,status,button) in enumerate([
        ('1 · First garden','Two beds · original farm','garden-first','Complete','View first field'),
        ('2 · Reclaim the field','Four beds · old stump area','garden','Current garden','Focus garden'),
        ('3 · Southern field','Six beds total · southeast addition','garden-third','Next expansion','View requirements')
    ]):
        x=48+i*397;b.rect(x,260,373,364,'selected' if i==1 else 'field','accent' if i==1 else 'line');b.text(name,x+14,273,345,28,20);b.text(subtitle,x+14,306,345,24,15,'text' if i==1 else 'muted');b.img(art,x+90,341,192,220);b.text(status,x+14,566,345,22,16,'green' if i==0 else 'text' if i==1 else 'muted','center');b.button(button,x+14,590,345,34,i==1,15)
    b.footer('Actual approved preview geometry. The third image shows the southern addition; stages 1/2 share a camera. XP and quest costs illustrative.');return b

if __name__=='__main__':
    import sys
    boards={'desktop':desktop,'mobile':mobile,'detail':detail,'watering':watering,'progression':progression}
    for key in sys.argv[1:] or boards:
        boards[key]().save()
