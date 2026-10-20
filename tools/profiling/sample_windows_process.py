import ctypes as c, ctypes.wintypes as w, time, pathlib, sys, csv, json
limit=float(sys.argv[3]) if len(sys.argv)>3 else 1200
pid=int(sys.argv[1]);root=pathlib.Path(sys.argv[2]);kernel=c.WinDLL('kernel32',use_last_error=True);psapi=c.WinDLL('psapi',use_last_error=True)
class Counters(c.Structure):
 _fields_=[('cb',w.DWORD),('PageFaultCount',w.DWORD),('PeakWorkingSetSize',c.c_size_t),('WorkingSetSize',c.c_size_t),('QuotaPeakPagedPoolUsage',c.c_size_t),('QuotaPagedPoolUsage',c.c_size_t),('QuotaPeakNonPagedPoolUsage',c.c_size_t),('QuotaNonPagedPoolUsage',c.c_size_t),('PagefileUsage',c.c_size_t),('PeakPagefileUsage',c.c_size_t),('PrivateUsage',c.c_size_t)]
kernel.OpenProcess.restype=w.HANDLE;kernel.OpenProcess.argtypes=[w.DWORD,w.BOOL,w.DWORD]
kernel.GetExitCodeProcess.argtypes=[w.HANDLE,c.POINTER(w.DWORD)];kernel.TerminateProcess.argtypes=[w.HANDLE,w.UINT];kernel.CloseHandle.argtypes=[w.HANDLE]
psapi.GetProcessMemoryInfo.argtypes=[w.HANDLE,c.POINTER(Counters),w.DWORD]
handle=kernel.OpenProcess(0x1000|0x10|0x1,False,pid)
if not handle:raise OSError(c.get_last_error())
start=time.monotonic();samples=0;ended='process-exited'
with (root/'process-memory.csv').open('w',newline='') as f:
 writer=csv.writer(f);writer.writerow(['utc','elapsed','phase','workingSet','privateCommit','lifetimePeakWorkingSet','lifetimePeakCommit'])
 while True:
  exitCode=w.DWORD();kernel.GetExitCodeProcess(handle,c.byref(exitCode))
  if exitCode.value!=259:break
  counters=Counters();counters.cb=c.sizeof(counters)
  phase='import-or-initialization'
  try:phase=json.loads((root/'status.json').read_text()).get('phase',phase)
  except (OSError,ValueError):pass
  if psapi.GetProcessMemoryInfo(handle,c.byref(counters),c.sizeof(counters)):
   from datetime import datetime,timezone
   writer.writerow([datetime.now(timezone.utc).isoformat(),time.monotonic()-start,phase,counters.WorkingSetSize,counters.PrivateUsage,counters.PeakWorkingSetSize,counters.PeakPagefileUsage]);samples+=1
  f.flush()
  if time.monotonic()-start>limit:
   ended='owned-profiling-player-timeout';kernel.TerminateProcess(handle,3);break
  time.sleep(.05)
kernel.CloseHandle(handle)
(root/'sampler-completion.json').write_text(json.dumps({'pid':pid,'samples':samples,'ended':ended,'seconds':time.monotonic()-start}))
