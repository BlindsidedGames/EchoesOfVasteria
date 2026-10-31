from pathlib import Path
import json
root=Path(__file__).resolve().parents[4];doc=root/'docs/development/watch-integration-20261101'
rows=[('01','expedition-dial','Expedition dial','dial'),('03','combat-watch','Health','health'),('04','harvest-counter','Resources','resources'),('05','since-you-left','Since you left','away'),('07','skillbook','Skillbook','skills'),('08','run-ribbon','Recent runs','history'),('09','run-comparison','Compare runs','comparison')]
designs=[]
for ident,slug,title,screen in rows:
 states=[{'title':p.stem.split('-',2)[-1]+' · '+size+'mm','file':str(p.relative_to(doc))} for size in ['42','46'] for p in sorted((doc/'captures').glob(ident+'-'+size+'-*.png')) if not p.stem.endswith(('initial','final'))]
 designs.append({'id':ident,'slug':slug,'title':title,'purpose':'Selected production Watch '+title+' screen','short':'Real companion views with actual snapshot contract and personalized catalog art.','interaction':'Top-right WatchConnectivity refresh; Run/Skills/History menu. See proof for verified input paths.','data':'Fixture-backed native view rendering; production app uses its real store and clock.','changes':['Reviewed lab layout integrated into production source.','Dynamic personalized layers replace the fixed lab character.'],'captures':{size:{'initial':f'captures/{ident}-{size}-initial.png','final':f'captures/{ident}-{size}-final.png'} for size in ['42','46']},'states':states,'clip':None,'clips':[]})
(doc/'manifest.json').write_text(json.dumps({'provenance':'Production SwiftUI views, isolated native fixture adapter, real contract. No production deployment or live-game transport claim.','designs':designs},indent=2)+'\n')
