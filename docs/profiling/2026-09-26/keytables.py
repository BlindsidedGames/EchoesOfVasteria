import json,csv,statistics,pathlib
p=pathlib.Path('docs/profiling/2026-09-26');d=json.loads((p/'summary.json').read_text())
groups=[('Fresh town','town-fresh-r'),('Developed, tasting stopped','town-developed-no-taste-r'),('Taste 10, closed','taste-10-closed-r'),('Taste 10, open','taste-10-open-r'),('Taste 100, closed','taste-100-closed-r'),('Taste 100, open','taste-100-open-r'),('Taste 1000 stress, closed','taste-1000-closed-r'),('Taste 1000 stress, open','taste-1000-open-r'),('Forge 20 stress, open','forge-auto-config-20-r'),('Forge 100 stress, open','forge-auto-config-100-r'),('Forge 1000 stress, open','forge-auto-config-1000-r'),('Forge native, cap 30','forge-native-cap30-r'),('Forge native, cap 60','forge-native-cap60-r'),('Forge native, cap 144','forge-native-cap144-r')]
lines=['| Editor scenario | Reps | Frame median | p95 | p99 | Worst | PlayerLoop p95 | GC KB/frame | Throughput per real s |','|---|---:|---:|---:|---:|---:|---:|---:|---|']
for label,pre in groups:
 rows=[r for r in d if r['scenario'].startswith(pre)]
 if not rows:continue
 def rng(key,scale=1):
  v=[r[key]*scale for r in rows if key in r and not (key=='crafts_per_s' and 'craft_warning' in r)]
  if not v:return '—'
  lo,hi=min(v),max(v);return f'{lo:.1f}' if len(v)==1 else f'{lo:.1f}–{hi:.1f}'
 rate='crafts_per_s' if label.startswith('Forge') else 'tastes_per_s'
 lines.append('| '+label+' | '+str(len(rows))+' | '+' | '.join(rng(k) for k in ['frame_median','frame_p95','frame_p99','frame_max','playerloop_p95'])+' | '+rng('gc_alloc_mean',.001)+' | '+rng(rate)+' |')
(p/'KeyResults.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
