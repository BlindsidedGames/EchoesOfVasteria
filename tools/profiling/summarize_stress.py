import csv,json,statistics,sys
from pathlib import Path
r=Path(sys.argv[1]);a=list(csv.DictReader((r/'activity.csv').open()));m=list(csv.DictReader((r/'memory.csv').open()));f=list(csv.DictReader((r/'frames.csv').open()));p=list(csv.DictReader((r/'process-memory.csv').open()))
phases=[]
def percentile(values,q):
 s=sorted(values);return s[min(len(s)-1,int((len(s)-1)*q))] if s else None
for name in dict.fromkeys(x['phase'] for x in a):
 rows=[x for x in a if x['phase']==name];start=float(rows[0]['seconds']);end=float(rows[-1]['seconds']);dt=end-start
 quiet=[x for x in f if start+5<=float(x['seconds'])<=end]
 phases.append({'phase':name,'seconds':dt,'samples':len(rows),'craftDelta':int(rows[-1]['autoCrafts'])-int(rows[0]['autoCrafts']),'tasteDelta':int(rows[-1]['tastings'])-int(rows[0]['tastings']),
 'craftsPerSecond':(int(rows[-1]['autoCrafts'])-int(rows[0]['autoCrafts']))/dt if dt else None,'tastesPerSecond':(int(rows[-1]['tastings'])-int(rows[0]['tastings']))/dt if dt else None,
 'allocatedFirstLastMiB':[int(rows[0]['allocated'])/2**20,int(rows[-1]['allocated'])/2**20],'managedFirstLastMiB':[int(rows[0]['managedUsed'])/2**20,int(rows[-1]['managedUsed'])/2**20],
 'graphicsFirstLastMiB':[int(rows[0]['graphics'])/2**20,int(rows[-1]['graphics'])/2**20],'telemetryMaxPending':max(int(x['telemetryPending']) for x in rows),
 'forgeInactiveSamples':sum(x['crafting']!='True' for x in rows),'tasteInactiveSamples':sum(x['tasting']!='True' for x in rows),
 'wallMedianMs':percentile([float(x['wallMs']) for x in quiet],.5),'wallP95Ms':percentile([float(x['wallMs']) for x in quiet],.95),'wallMaxMs':max([float(x['wallMs']) for x in quiet],default=None),
 'framesOver33ms':sum(float(x['wallMs'])>33.333 for x in quiet),'measuredFrames':len(quiet),
 'infinitySamples':sum(x['infinityActive']=='True' for x in rows),'restarts':int(rows[-1]['restarts']),'farmSequenceDelta':int(rows[-1]['farmSequence'])-int(rows[0]['farmSequence'])})
out={'run':str(r),'contract':json.loads((r/'stress-contract.json').read_text()),'completion':json.loads((r/'stress-completion.json').read_text()),'phases':phases,
 'windowsFullRunWorkingSetPeakMiB':max(int(x['lifetimePeakWorkingSet']) for x in p)/2**20,'windowsFullRunPrivateCommitPeakMiB':max(int(x['lifetimePeakCommit']) for x in p)/2**20,
 'notes':['Synthetic high-resource state; actual production counters prove activity.','Windows GPU/Unity/process accounting remains separate.','Steady frame statistics exclude first five seconds of each UI transition, not evidence all transitions are smooth.']}
(r/'stress-summary.json').write_text(json.dumps(out,indent=2));print(json.dumps(out,indent=2))
