from pathlib import Path
import subprocess,time,sys,json
root=Path(__file__).resolve().parents[4];out=root/'docs/development/watch-integration-20261101/captures';out.mkdir(parents=True,exist_ok=True)
app=root/'Artifacts/WatchIntegration/DerivedData/Build/Products/Debug-watchsimulator/EchoesWatchPreview.app';bundle='com.blindsidedgames.echoes.watch.integration.preview'
revision=sys.argv[1] if len(sys.argv)>1 else 'initial'
ids={'42':'6EFA4422-289B-471C-A2B8-B2ADEDE14CDA','46':'00B97A91-5573-4DFA-827B-0783D7096191'}
cases=[('01','dial','live'),('03','health','live'),('04','resources','live'),('05','away','away'),('07','skills','live'),('08','history','history'),('09','comparison','history')]
for size,device in ids.items():
 subprocess.run(['xcrun','simctl','install',device,str(app)],check=True)
 for ident,screen,state in cases:
  subprocess.run(['xcrun','simctl','launch','--terminate-running-process',device,bundle,'--screen='+screen,'--state='+state],check=True,stdout=subprocess.DEVNULL)
  time.sleep(3 if ident=="01" else 1)
  file=out/f'{ident}-{size}-{revision}.png'
  subprocess.run(['xcrun','simctl','io',device,'screenshot',str(file)],check=True,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
  print(file.name,flush=True)
