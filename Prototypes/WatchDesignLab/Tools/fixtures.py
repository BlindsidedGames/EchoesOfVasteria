from pathlib import Path
import json,copy,hashlib
lab=Path(__file__).resolve().parents[1]; base=json.loads((lab/'Fixtures/baseline.json').read_text()); notes={}
def save(name,s,note):
    (lab/'Fixtures'/(name+'.json')).write_text(json.dumps(s,separators=(',',':'))+'\n');notes[name]=note
live=copy.deepcopy(base);live['phoneInBackground']=False;save('live',live,'Actual Unity-produced saved run, portrait, skills and history. Only phoneInBackground false for foreground presentation.')
for ending in ['dead','reaped']:
    ended=copy.deepcopy(live);ended['run'][ending]=True
    ended['run']['healthFraction']=0
    if ending=='reaped': ended['run'].update(distance=ended['run']['cap'],dead=True)
    save(ending,ended,'Controlled ended run: explicit status, static portrait, no invented cumulative reaps.')
away=copy.deepcopy(base);away['forecast']=None
away['timeline']['horizonSeconds']=300
away['timeline']['frames']=[[0,48,212,0,0,0,0,0],[100,212,212,75,0,0,40,8],[100,0,212,75,1,1,40,8],[120,0,212,75,1,1,40,8],[120,0,212,75,1,0,40,8],[250,190,212,160,1,0,90,20],[250,190,212,160,1,2,90,20],[300,190,212,160,1,2,90,20]]
# phase2 is terminal and must be the final row, so horizon ends at the transition.
away['timeline']['horizonSeconds']=300;away['timeline']['frames']=away['timeline']['frames'][:-2]+[[300,212,212,180,1,0,100,22],[300,212,212,180,1,2,100,22]]

for frame in away['timeline']['frames']:
    if frame[4] >= 1: frame[2]=214.12
save('away',away,'Controlled eight-column trajectory for gained counters, reap/restart pause and expiry. Not an observed forecast.')
largeaway=copy.deepcopy(away)
for frame in largeaway['timeline']['frames']:
    frame[3]*=1000000
save('large-away',largeaway,'Controlled trajectory resource gains scaled one million times to stress compact values.')
for name in ['restart','stale','offline']:
    save(name,copy.deepcopy(away),f'Same controlled trajectory, sampled at {"110s restart" if name=="restart" else "7200s expired" if name=="stale" else "15s cached offline"}.')
no=copy.deepcopy(live);no['run']['distanceReaping']=False;no['run']['cap']=0;no['timeline']=None;no['forecast']=None;save('no-reap',no,'Controlled distance reaping disabled; no distance cap or percent may appear.')
large=copy.deepcopy(live);large['run'].update(distance=1234567,cap=38000000,resources=987654321,tasks=123456,kills=98765,durationSeconds=123456,healthFraction=.12);save('large',large,'Controlled large values and low health, real source shape.')
town=copy.deepcopy(live);town.update(state='town',run=None,timeline=None,forecast=None);save('town',town,'Controlled town state; retained skills/history remain available.')
history=copy.deepcopy(live)
rows=[('River','River',180,212,350,'reaped',False),('Mines','Mines',280,2100,420,'died',False),('River','River',200,212,390,'reaped',True),('Woods','Woods',110,740,190,'abandoned',False),('River','River',210,212,480,'reaped',False),('Mines','Mines',300,2400,570,'died',True),('Woods','Woods',150,990,240,'completed',False),('River','River',240,212,550,'reaped',True),('Mines','Mines',310,2550,610,'completed',False),('River','River',260,212,630,'reaped',False)]
history['recentRuns']=[dict(runNumber=17+i,mapId=m,mapTitle=t,durationSeconds=sec,distance=d,resources=r,tasks=20+i*3,kills=2+i,result=result,estimated=est) for i,(m,t,sec,d,r,result,est) in enumerate(rows)][::-1]
historylarge=copy.deepcopy(history)
historylarge['run']=copy.deepcopy(large['run'])
for row in historylarge['recentRuns']:
    row['distance']*=1000;row['resources']*=1000000
save('large-history',historylarge,'Controlled retained-history distances scaled1000 and resources one million times for compact chart/comparison/map stress.')
save('history',history,'CONTROLLED varied retained global ten for trends/comparison/dossier, not real player history. Same-map records include estimated and recorded outcomes.')
mapgap=copy.deepcopy(history);mapgap['run'].update(mapId='Farmlands',mapTitle='Farmlands');mapgap['timeline']=None;mapgap['forecast']=None
save('map-gap',mapgap,'Controlled known current Farmlands run with no retained Farmlands records. Does not reveal undiscovered maps.')
townempty=copy.deepcopy(town);townempty['recentRuns']=[]
save('town-empty',townempty,'Controlled town with no current map or retained records; no known map selector.')
nohist=copy.deepcopy(history);nohist['recentRuns']=[];save('no-history',nohist,'Controlled no retained history; current River map known, no invented best/rate.')
beyond=copy.deepcopy(history);beyond['run'].update(mapId='Mines',mapTitle='Mines',distance=5200,cap=38000);beyond['timeline']=None;beyond['forecast']=None;save('beyond-history',beyond,'Controlled current Mines distance 5200 beyond recent best2550, actual effective cap38000. Does not assert managed estimator behavior.')
(lab/'FixtureProvenance.json').write_text(json.dumps({'baselineSHA256':hashlib.sha256((lab/'Fixtures/baseline.json').read_bytes()).hexdigest(),'baselineSource':'Actual Unity helmet snapshot recorded 2026-10-31, build188 character integration. Source retained verbatim.','variants':notes,'clock':'Frozen source generatedAt +15 seconds; away +85; restart +110; stale +7200. Animation clock only is real time. Empty uses nil.'},indent=2)+'\n')
