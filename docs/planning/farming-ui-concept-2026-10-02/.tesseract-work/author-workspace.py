from author import Board

class Workspace(Board):
    def shell(self,tab):
        self.chrome()
        self.img('Flora',49,103,32,38);self.img('Tillman',85,103,32,38)
        self.text('Flora & Tillman',136,102,480,36,20)
        self.button('Garden',876,102,160,38,True,16)
        self.rect(1044,102,184,38,'control','line','Locked Orchard switch')
        self.text('Orchard · Locked',1052,102,168,38,16,'muted','center')
        self.rect(48,154,1180,1,'line',name='Workspace header divider')
        for name,x,w in [('Beds',48,120),('Progression',176,164),('Town',348,120)]:self.button(name,x,170,w,38,name==tab,16)
    def bed_row(self,n,crop,status,action,y,selected=False):
        x=410;w=818;h=58
        if selected:self.rect(x-6,y-1,w+6,h,'selected','accent','Selected bed')
        self.text(str(n),x,y+5,30,30,18,'text' if selected else 'muted','center')
        if crop not in ['Empty','Locked']:self.img('SeedPack_'+crop+'-2x',x+44,y+9,32,32)
        else:self.rect(x+51,y+15,18,18,'field','line','Empty bed marker')
        self.text(crop,x+92,y+3,460,25,18)
        if status:self.text(status,x+92,y+28,460,22,14,'green' if status=='Ready' else 'muted')
        if action:self.button(action,1090,y+9,138,38,action=='Harvest',16)
        if not selected:self.rect(x,y+h-1,w,1,'line',name='Bed row divider')
    def discovery_grid(self,x,y):
        known=['Radish','Corn','Carrot','Wheat']
        for i in range(17):
            name=known[i] if i<4 else 'Unknown';label=name if i<4 else '???';xx=x+(i%6)*112;yy=y+(i//6)*108
            self.rect(xx,yy,106,102,'field','line',label+' discovery slot')
            self.img('SeedPack_'+name+'-3x',xx+29,yy+12,48,48)
            self.text(label,xx+4,yy+70,98,22,14,'text','center')

def beds():
    b=Workspace('Garden-Beds-desktop');b.shell('Beds')
    b.text('Seeds',48,221,320,28,20);b.seed_grid(48,261,tw=81,th=91);b.button('All seeds',48,552,320,38,False,16)
    b.text('Beds',410,221,610,28,20);b.text('4 / 6',1090,221,138,28,16,'muted','right')
    for n,crop,status,action in [(1,'Carrot','Ready','Harvest'),(2,'Corn','Growing · 12 min','Details'),(3,'Empty','',None),(4,'Wheat','Ready','Harvest'),(5,'Locked','', 'Details'),(6,'Locked','', 'Details')]:b.bed_row(n,crop,status,action,261+(n-1)*60,n==3)
    b.text('Radish → Bed 3',410,640,640,28,18);b.button('Plant',1090,631,138,42,True,16)
    return b

def progression():
    b=Workspace('Garden-Progression-desktop');b.shell('Progression')
    b.text('Access',48,228,420,28,20)
    for label,state,y in [('Garden','Available',278),('Orchard','Locked',330)]:
        b.text(label,48,y,220,28,18);b.text(state,270,y,194,28,16,'green' if state=='Available' else 'muted','right');b.rect(48,y+41,416,1,'line')
    b.text('Beds',48,398,420,28,20)
    for label,state,y in [('1–2','Available',444),('3–4','Available',498),('5–6','Locked',552)]:
        b.text(label,48,y,220,28,18);b.text(state,270,y,194,28,16,'green' if state=='Available' else 'muted','right');b.rect(48,y+41,416,1,'line')
    b.text('Seed discoveries',530,228,470,28,20);b.text('4 / 17',1090,228,106,28,16,'muted','right');b.discovery_grid(530,276)
    return b

def town():
    b=Workspace('Garden-Town-desktop');b.shell('Town')
    b.img('garden-town-workspace',48,221,824,464)
    for n,x,y in [(1,278,304),(2,356,304),(3,257,503),(4,377,503)]:
        b.rect(x,y,24,26,'surface','line','World bed '+str(n));b.text(str(n),x,y,24,26,18,'text','center')
    b.text('Focus',904,228,324,28,20);b.button('Garden',904,276,324,42,True,16)
    for n,x,y in [(1,904,338),(2,1072,338),(3,904,392),(4,1072,392)]:b.button('Bed '+str(n),x,y,156,42,False,16)
    return b

if __name__=='__main__':
    import sys
    boards={'beds':beds,'progression':progression,'town':town}
    for key in sys.argv[1:] or boards:boards[key]().save()
