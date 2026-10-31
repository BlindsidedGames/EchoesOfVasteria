from pathlib import Path
import subprocess,json,time,os,signal,sys,datetime,hashlib
R=Path(__file__).resolve().parent;trial=sys.argv[1];E=R/trial;E.mkdir(exist_ok=False)
PHONE='B9CAF457-C4E9-454A-BC0B-AD7D765A1CDC';BUNDLE='com.blindsidedgames.timeformatlab'
def call(args,**kwargs):return subprocess.check_output(args,text=True,**kwargs).strip()
def save(n,d):(E/n).write_text(json.dumps(d,indent=2)+'\n')
# Only this disposable harness is launched or signalled; no existing game app/data is changed.
container=Path(call(['xcrun','simctl','get_app_container',PHONE,BUNDLE,'data']));receipt=container/'Documents/start.json';events=container/'Documents/events.jsonl'
old=json.loads(receipt.read_text()).get('run') if receipt.exists() else None
logFile=(E/'renderer.log').open('w');log=subprocess.Popen(['xcrun','simctl','spawn',PHONE,'log','stream','--style','compact','--level','debug','--predicate','process == "chronod" OR process == "liveactivitiesd" OR process CONTAINS "TimeFormat" OR subsystem CONTAINS "SwiftUI" OR eventMessage CONTAINS "Codable" OR eventMessage CONTAINS "LabAttributed" OR eventMessage CONTAINS "LabString" OR (process == "SpringBoard" AND (eventMessage CONTAINS[c] "timeformat" OR eventMessage CONTAINS[c] "decode" OR eventMessage CONTAINS[c] "render"))'],stdout=logFile,stderr=subprocess.STDOUT)
start=time.time();launch=call(['xcrun','simctl','launch','--terminate-running-process',PHONE,BUNDLE,'--trial',trial]);(E/'launch.txt').write_text(launch);pid=int(launch.rsplit(':',1)[1]);save('launcher.json',{'launchAt':start,'pid':pid,'debuggerAttached':False,'cliLaunch':launch})
end=time.monotonic()+12
while time.monotonic()<end:
 try:
  d=json.loads(receipt.read_text())
  if d['run']!=old and d['requestCompletedAt']>=start-1:break
 except (OSError,ValueError):pass
 time.sleep(.1)
else:raise SystemExit('No fresh activity request receipt')
save('request.json',d);origin=d['origin'];print(json.dumps(d),flush=True)
videoFile=(E/'video-stderr.log').open('w');video=subprocess.Popen(['xcrun','simctl','io',PHONE,'recordVideo','--codec=h264',str(E/'lockscreen.mp4')],stdout=videoFile,stderr=subprocess.STDOUT)
# Wait until the simulator UI backgrounds the host, then prove zero host execution.
end=time.monotonic()+30;background=None
while time.monotonic()<end:
 for line in events.read_text().splitlines() if events.exists() else []:
  j=json.loads(line)
  if j.get('at',0)>=start and j.get('event')=='phase' and j.get('phase')=='background':background=j
 if background:break
 time.sleep(.1)
if not background:raise SystemExit('No host background phase; trial not valid')
command=call(['ps','-p',str(pid),'-o','command=']);assert '/TimeFormatLab.app/TimeFormatLab' in command,command
os.kill(pid,signal.SIGSTOP);save('host-stopped.json',{'backgroundEvent':background,'stoppedAt':time.time(),'pid':pid,'command':command,'ps':call(['ps','-p',str(pid),'-o','pid=,state=,time=,pcpu=,command='])})
records=[]
for target in ([0,3,8,15,30,45] if trial == "control" else [0,3,6]):
 while time.time()<origin+target:time.sleep(min(.1,max(.001,origin+target-time.time())))
 observed=time.time();filename=f'lockscreen-t{target:02}.png'
 subprocess.run(['xcrun','simctl','io',PHONE,'screenshot',str(E/filename)],check=True,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
 ps=call(['ps','-p',str(pid),'-o','pid=,state=,time=,pcpu=,command=']);assert ' T ' in ' '+ps+' ' or ' T+' in ps or ' TX' in ps or ' T' in ps,ps
 elapsed=observed-origin;tick=max(0,int(elapsed));phase=(tick-4)%22 if tick>=4 else None
 restart=(180+10*tick if tick<2 else 200 if tick<4 or phase>=20 else phase*10)
 records.append({'targetElapsedSeconds':target,'capturedAtUnix':observed,'capturedAtUTC':datetime.datetime.fromtimestamp(observed,datetime.timezone.utc).isoformat(),'actualElapsedSeconds':elapsed,'expectedLongDistance':2000+10*tick,'expectedResources':2*tick,'expectedRestartDistance':restart,'nativeLongExpectedFraction':min(1,(200+max(0,elapsed))/3830),'nativeFastExpectedFraction':min(1,(2+max(0,elapsed))/10),'nativeShortExpectedFraction':min(1,(18+max(0,elapsed))/20),'hostPS':ps,'screenshot':filename})
 save('checkpoints.json',records);print('captured '+filename,flush=True)
video.send_signal(signal.SIGINT)
try:video.wait(timeout=15)
except subprocess.TimeoutExpired:video.terminate();video.wait(timeout=5)
log.terminate()
try:log.wait(timeout=10)
except subprocess.TimeoutExpired:log.kill()
(E/'host-events.jsonl').write_bytes(events.read_bytes());save('completed.json',{'completedAt':time.time(),'hostLeftStopped':True,'publisherUpdates':0,'trial':trial})
