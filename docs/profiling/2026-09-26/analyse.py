import csv,json,pathlib,statistics,sys,zipfile,io
root=pathlib.Path(sys.argv[1] if len(sys.argv)>1 else 'docs/profiling/2026-09-26')
def pct(v,p):
 v=sorted(v); x=(len(v)-1)*p; i=int(x); return v[i]+(v[min(i+1,len(v)-1)]-v[i])*(x-i)
def summary():
 live={r['name']:r for r in json.loads((root/'throughput-observations.json').read_text())} if (root/'throughput-observations.json').exists() else {}
 crafts={r['scenario']:r for r in csv.DictReader((root/'craft-throughput.csv').open())} if (root/'craft-throughput.csv').exists() else {}
 out=[]
 archive=zipfile.ZipFile(root/'frame-captures.zip') if (root/'frame-captures.zip').exists() else None
 paths=set(root.glob('*.csv'))
 if archive: paths.update(root/name for name in archive.namelist() if name.endswith('.csv'))
 for path in sorted(paths):
  meta=root/(path.stem+'-meta.json')
  if not meta.exists():continue
  stream=path.open() if path.exists() else io.TextIOWrapper(archive.open(path.name))
  with stream: raw=list(csv.DictReader(stream))
  data=raw[2:] # Boundary enumeration and previous-frame recorder lag.
  if not data:continue
  m=json.loads(meta.read_text()); row={'scenario':path.stem,'frames':len(data),'seconds':m['capture_s'],'warmup_s':m['warmup_s'],'context':'Development Player' if path.stem.startswith('player-') else 'Editor','raw_frame_max_including_boundary':max(float(r['unscaledDelta_ms']) for r in raw)}
  for key,scale,label in [('wall_ms',1,'wall'),('unscaledDelta_ms',1,'frame'),('Main Thread',1e-6,'main'),('Render Thread',1e-6,'render'),('PlayerLoop',1e-6,'playerloop'),('BehaviourUpdate',1e-6,'behaviour'),('GPU Frame Time',1e-6,'gpu'),('Canvas.BuildBatch',1e-6,'canvas'),('GC Allocated In Frame',1,'gc_alloc'),('GC Used Memory',1,'gc_used'),('Total Used Memory',1,'memory'),('drawCalls',1,'drawcalls'),('legacyBatches',1,'batches'),('Batches Count',1,'batches_counter'),('frameTimingMain_ms',1,'ft_main'),('frameTimingRender_ms',1,'ft_render'),('frameTimingGPU_ms',1,'ft_gpu')]:
   if key not in data[0]:continue
   vals=[float(r[key])*scale for r in data if float(r[key])>=0]
   if not vals:continue
   if label in ['gpu','render','ft_main','ft_render','ft_gpu','drawcalls','batches_counter'] and max(vals)==0:continue # An all-zero unavailable timing is not a zero-cost measurement.
   for p,n in [(0.5,'median'),(.95,'p95'),(.99,'p99'),(1,'max')]:row[label+'_'+n]=pct(vals,p)
   row[label+'_mean']=statistics.mean(vals)
  b,a=m['before'],m['after'];dt=a['time']-b['time'];row['actual_seconds']=dt;span=float(data[-1]['elapsed_s'])-float(data[0]['elapsed_s']);row['sample_span_s']=span;row['sampled_frames_per_s']=(len(data)-1)/span if span>0 else 0;row['gc_alloc_bytes_per_s']=row.get('gc_alloc_mean',0)*row['sampled_frames_per_s']
  row['tastes_per_s']=(a['tastes']-b['tastes'])/dt
  if row['context']=='Development Player':
   for field,label in [('TotalKills','kills'),('TasksCompleted','tasks'),('crafts','crafts')]:row[label+'_per_s']=(a[field]-b[field])/dt
   row['throughput_source']='live runtime counters'
  else:
   row['throughput_source']='not observed; save counters deliberately omitted'
   if path.stem in live:
    v=live[path.stem]; span=v['end']-v['start']; row['throughput_seconds']=span
    if span>0:
     row.update({f'{k}_per_s':(v[k+'End']-v[k+'Start'])/span for k in ['tasks','kills']})
     row.update({k:v[k] for k in ['distanceStart','distanceEnd','echoesStart']});row['throughput_source']='live GameplayStatTracker; one-frame boundary uncertainty'
   if path.stem in crafts:
    v=crafts[path.stem];row['crafts_per_s']=float(v['crafts_per_s']);row['craft_observation_seconds']=float(v['seconds'])
    if float(v['seconds'])<dt*.95:row['craft_warning']='partial capture; exclude rate comparison'
  for key in ['objects','gameObjects','mono','allocated','reserved','gc0','gc1','gc2']:
   if key in b:row[key+'_before']=b[key];row[key+'_after']=a[key];row[key+'_delta']=a[key]-b[key]
  for key in ['fps','vsync','width','height','map','CraftingMasteryLevel','CauldronEvaLevel','cards','resources','quests','histories']:
   row[key]=b.get(key)
  for key in ['timeScale','inRun','forgeAuto','IsTasting']:row[key+'_start']=b[key];row[key+'_end']=a[key]
  setup=root/(path.stem+'-setup.json')
  if setup.exists():row['setup_action_ms']=json.loads(setup.read_text())['actionMs']
  out.append(row)
 if out:
  keys=list(dict.fromkeys(k for r in out for k in r))
  with (root/'summary.csv').open('w',newline='') as f:w=csv.DictWriter(f,fieldnames=keys);w.writeheader();w.writerows(out)
  (root/'summary.json').write_text(json.dumps(out,indent=2))
  lines=['# Per-scenario measurements','', 'All times are milliseconds. First two boundary samples are excluded from percentiles. Raw captures retain them. Empty throughput means not observed; zero means observed zero. Cold/transition windows and timeline/probe scenarios must not be treated as steady-state. See the main report for fixture definitions and limitations.','', '| Scenario | Warm/capture s | Frame median / p95 / p99 / worst | PlayerLoop p95 | GC bytes/frame | Tasks / kills / crafts / rolls per real s | VSync / cap / scale |','|---|---:|---:|---:|---:|---:|---|']
  def fmt(r,k):return f'{r[k]:.2f}' if k in r else '—'
  for r in out:
   lines.append('| '+r['scenario']+' | '+str(r['warmup_s'])+'/'+str(r['seconds'])+' | '+' / '.join(fmt(r,'frame_'+p) for p in ['median','p95','p99','max'])+' | '+fmt(r,'playerloop_p95')+' | '+fmt(r,'gc_alloc_mean')+' | '+' / '.join(fmt(r,k+'_per_s') for k in ['tasks','kills','crafts','tastes'])+' | '+f"{r['vsync']} / {r['fps']} / {r['timeScale_start']:.2f}–{r['timeScale_end']:.2f}"+' |')
  (root/'ScenarioResults.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
 return out
if __name__=='__main__':
 out=summary();print(f'{len(out)} scenarios summarized under {root}')
