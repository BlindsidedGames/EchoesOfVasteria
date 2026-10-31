#!/usr/bin/env python3
"""Retain native recordings, shorten the goal proof, and hold static VFR frames for playback."""
from pathlib import Path
import subprocess,json,shutil,hashlib
root=Path(__file__).resolve().parents[3];doc=root/'docs/development/watch-design-lab-20261031';clips=doc/'clips';raw=clips/'raw';raw.mkdir(exist_ok=True)
requested={'02-42-foreground-animation':6,'02-46-foreground-animation':6,'03-42-background-static':6,'02-42-dimmed-static':4,'02-42-always-on-transition':10}
rows=[]
for output in sorted(clips.glob('*.mp4')):
 source=raw/output.name
 if not source.exists():shutil.copy2(output,source)
 meta=json.loads(subprocess.check_output(['ffprobe','-v','error','-show_entries','stream=width,height,nb_frames','-show_entries','format=duration','-of','json',str(source)]))
 transform='Unmodified native simulator recording.'
 if output.stem=='06-42-goal-editor':
  subprocess.run(['ffmpeg','-v','error','-y','-i',str(source),'-t','18','-c','copy','-movflags','+faststart',str(output)],check=True);transform='First18 seconds, codec stream copy. Native100→500 resource preset interaction; full77.5s original retained.'
 elif output.stem in ['03-42-background-static','02-42-dimmed-static']:
  seconds=requested[output.stem]+.5
  subprocess.run(['ffmpeg','-v','error','-y','-i',str(source),'-vf',f'fps=30,tpad=stop_mode=clone:stop_duration={seconds}','-t',str(seconds),'-c:v','libx264','-preset','fast','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',str(output)],check=True)
  transform=f'Playback hold of last captured frame to{seconds}s. NativeVFR emitted no further changing frames during requested{requested[output.stem]}s observation; no generated motion or interpolation. Original retained.'
 rows.append({'clip':str(output.relative_to(doc)),'raw':str(source.relative_to(doc)),'rawSHA256':hashlib.sha256(source.read_bytes()).hexdigest(),'nativeMetadata':meta,'requestedObservationSeconds':requested.get(output.stem),'presentationTransform':transform})
(doc/'video-provenance.json').write_text(json.dumps(rows,indent=2)+'\n')
print('Packaged',len(rows),'native clips with original recordings and explicit playback transforms')
