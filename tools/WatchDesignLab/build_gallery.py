"""Build a local review page from native Watch design-lab captures."""
from pathlib import Path
import argparse
import json


PAGE = r'''<!doctype html>
<html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Watch companion designs</title>
<style>
:root{color-scheme:dark;--bg:#111b1f;--panel:#18262c;--line:#34474d;--ink:#eff2ef;--sub:#b5c2c3;--gold:#edbf71}
*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--ink);font:16px/1.5 system-ui,sans-serif}main{max-width:1280px;margin:auto;padding:34px 28px 70px}h1{font-size:34px;letter-spacing:-.8px;margin:0 0 10px}h2{font-size:25px;letter-spacing:-.4px;margin:0 0 10px}h3{font-size:18px;margin:0 0 8px}p{margin:0 0 14px;color:var(--sub);max-width:860px}a{color:var(--gold)}button,select{font:inherit;color:var(--ink);background:#213239;border:1px solid #567078;border-radius:7px;padding:10px 13px;min-height:44px}button{cursor:pointer}button:hover,select:hover{border-color:var(--gold)}button:focus-visible,select:focus-visible,a:focus-visible{outline:3px solid var(--gold);outline-offset:3px}button[aria-pressed=true]{color:var(--gold);border-color:var(--gold);background:#344039}.controls{display:flex;flex-wrap:wrap;gap:18px;align-items:end;margin:26px 0}.control{display:flex;flex-direction:column;gap:6px}.control label{font-size:13px;color:var(--sub)}.control select{min-width:150px}#design{min-width:290px}.layout{display:grid;grid-template-columns:minmax(400px,1.4fr) minmax(280px,1fr);gap:32px;align-items:start}.captures{background:#0a1013;border:1px solid var(--line);border-radius:14px;padding:26px;display:flex;justify-content:center;gap:30px;min-height:420px;align-items:center;flex-wrap:wrap}.capture{margin:0;max-width:280px;flex:1 1 180px;text-align:center}.capture img{display:block;width:100%;height:auto;image-rendering:auto;border-radius:0;background:#000}.capture a{display:block}.capture figcaption{margin:13px 0 0;color:var(--sub);font-size:13px}.capture em{display:block;font-size:12px;font-style:normal}.missing{padding:40px;color:var(--sub)}.review{padding-top:2px}.review ul{padding-left:20px;margin:0 0 18px;color:var(--sub)}.note{border-left:3px solid var(--gold);padding:0 0 0 14px;margin:20px 0}.state-links{display:flex;gap:9px;flex-wrap:wrap;margin:12px 0 22px}.state-links a{display:inline-block;padding:8px 11px;border:1px solid var(--line);border-radius:6px;font-size:13px;text-decoration:none}.state-links a:hover{border-color:var(--gold)}video{display:block;width:100%;max-height:480px;background:#000;border-radius:12px}.overview{margin-top:52px}.grid{display:grid;grid-template-columns:repeat(5,1fr);gap:15px;margin:20px 0}.thumb{padding:15px 12px;text-align:left;display:flex;flex-direction:column;gap:11px;min-width:0;background:var(--panel);border:1px solid var(--line)}.thumb img{width:100%;height:auto;max-height:220px;object-fit:contain;background:#000;border-radius:0}.thumb strong{font-size:14px;line-height:1.3}.thumb small{font-size:12px;color:var(--sub)}.native{font-size:13px;margin-top:12px}.sources{margin-top:44px;padding-top:20px;border-top:1px solid var(--line);font-size:14px;color:var(--sub)}.sources ul{padding-left:20px}details{margin-top:14px}summary{cursor:pointer;min-height:44px;padding:9px 0;color:var(--gold)}.nav{display:flex;gap:8px}.small{font-size:13px}.empty{opacity:.65}
@media(max-width:900px){.layout{grid-template-columns:1fr}.grid{grid-template-columns:repeat(3,1fr)}.captures{min-height:0}.review{max-width:700px}}
@media(max-width:520px){main{padding:24px 17px 50px}h1{font-size:29px}.controls{gap:12px}.control{flex:1}.control select,#design{min-width:0;width:100%}.captures{gap:18px;padding:18px}.capture{flex-basis:135px}.grid{grid-template-columns:repeat(2,1fr)}}
</style>
<main><h1>Watch companion designs</h1>
<p>Ten different ways to check an adventure from your wrist. These are native watchOS simulator captures, using the game’s pixel art. The shipped Watch app is unchanged.</p>
<div class="controls"><div class="control"><label for="design">Design</label><select id="design"></select></div>
<div class="control"><label for="size">Watch size</label><select id="size"><option value="both">Compare both</option><option value="42">42 mm</option><option value="46">46 mm</option></select></div>
<div class="control"><label for="iteration">Iteration</label><select id="iteration"><option value="final">Refined</option><option value="initial">First capture</option></select></div>
<div class="nav"><button id="prev" aria-label="Previous design">←</button><button id="next" aria-label="Next design">→</button><button id="overview">All ten</button></div></div>
<section class="layout" aria-live="polite"><div><div id="captures" class="captures"></div><p class="native">Tap a screenshot to inspect its original pixels.</p></div><div class="review"><h2 id="title"></h2><p id="purpose"></p><p id="interaction"></p><div id="recommendation" class="note"></div><h3>What changed</h3><ul id="changes"></ul><div id="states" class="state-links"></div><div id="motion"></div><details><summary>Data and limits</summary><p id="data" class="small"></p></details></div></section>
<section class="overview" id="all"><h2>All ten designs</h2><p>Choose a design to compare its sizes and revisions.</p><div class="grid" id="grid"></div></section>
<section class="sources"><p id="provenance"></p><p>Real-time animation is possible while the Watch app is active. Wrist-down and Reduce Motion use a still, simpler view. Phone snapshots and background estimates remain distinct.</p><ul><li><a href="https://developer.apple.com/documentation/watchos-apps/updating-watchos-apps-with-timelines">Apple: Watch timelines and animation</a></li><li><a href="https://developer.apple.com/documentation/watchos-apps/designing-your-app-for-the-always-on-state/">Apple: Always On behaviour</a></li><li><a href="REVIEW.md">Review evidence and limits</a></li></ul></section></main>
<script>
const study=__DATA__;
const designs=study.designs;
const $=id=>document.getElementById(id);
let selected=0;
const params=new URLSearchParams(location.search);
const requested=params.get('design');
if(requested){const found=designs.findIndex(d=>String(d.id)===requested||d.slug===requested);if(found>=0)selected=found;}
if(['42','46','both'].includes(params.get('size'))) $('size').value=params.get('size');
designs.forEach((d,i)=>{const option=document.createElement('option');option.value=i;option.textContent=String(d.id).padStart(2,'0')+' · '+d.title;$('design').append(option);});
function text(id,value){$(id).textContent=value||'';}
function files(d,size,iteration){return d.captures?.[size]?.[iteration]||d[size]?.[iteration]||null;}
function render(){
const d=designs[selected];$('design').value=selected;
text('title',d.title);text('purpose',d.purpose);text('interaction',d.interaction);text('recommendation',d.recommendation);$('recommendation').hidden=!d.recommendation;text('data',d.data);
$('changes').replaceChildren();for(const change of d.changes||[]){const li=document.createElement('li');li.textContent=change;$('changes').append(li);}
$('captures').replaceChildren();const sizes=$('size').value==='both'?['42','46']:[$('size').value];
for(const size of sizes){const path=files(d,size,$('iteration').value);const figure=document.createElement('figure');figure.className='capture';figure.style.flex='0 1 '+(size==='42'?234:260)+'px';
if(path){const link=document.createElement('a');link.href=path;link.target='_blank';link.rel='noopener';const img=new Image();img.src=path;img.alt=d.title+' on '+size+' mm Watch, '+$('iteration').value;link.append(img);figure.append(link);}
else{const missing=document.createElement('div');missing.className='missing';missing.textContent='Capture unavailable';figure.append(missing);}
const caption=document.createElement('figcaption');caption.textContent=size+' mm';const sub=document.createElement('em');sub.textContent=$('iteration').value==='final'?'Refined native capture':'First native capture';caption.append(sub);figure.append(caption);$('captures').append(figure);}
$('states').replaceChildren();for(const state of d.states||[]){const a=document.createElement('a');a.href=state.file;a.textContent=state.title;a.target='_blank';a.rel='noopener';$('states').append(a);}
$('motion').replaceChildren();for(const clip of d.clips?.length?d.clips:(d.clip?[d.clip]:[])){const figure=document.createElement('figure');figure.style.margin='0 0 18px';const v=document.createElement('video');v.src=clip;v.controls=true;v.loop=true;v.muted=true;v.playsInline=true;v.preload='metadata';const caption=document.createElement('figcaption');caption.className='small';caption.textContent=clip.split('/').pop().replace(/\.mp4$/,'').replace(/^\d+-/,'').replace(/-/g,' ');figure.append(v,caption);$('motion').append(figure);}
document.querySelectorAll('.thumb').forEach((b,i)=>b.setAttribute('aria-pressed',i===selected?'true':'false'));
const url=new URL(location);url.searchParams.set('design',d.slug||d.id);url.searchParams.set('size',$('size').value);history.replaceState({},'',url);
}
for(const [i,d] of designs.entries()){const button=document.createElement('button');button.className='thumb';button.type='button';const img=new Image();img.src=files(d,'42','final')||files(d,'46','final')||'';img.alt='';img.loading='lazy';const title=document.createElement('strong');title.textContent=String(d.id).padStart(2,'0')+' · '+d.title;const sub=document.createElement('small');sub.textContent=d.short||'';button.append(img,title,sub);button.addEventListener('click',()=>{selected=i;render();window.scrollTo({top:0,behavior:matchMedia('(prefers-reduced-motion: reduce)').matches?'instant':'smooth'});});$('grid').append(button);}
$('design').addEventListener('change',()=>{selected=Number($('design').value);render();});$('size').addEventListener('change',render);$('iteration').addEventListener('change',render);$('prev').addEventListener('click',()=>{selected=(selected+designs.length-1)%designs.length;render();});$('next').addEventListener('click',()=>{selected=(selected+1)%designs.length;render();});$('overview').addEventListener('click',()=>$('all').scrollIntoView({behavior:'smooth'}));
text('provenance',study.provenance||'Native simulator captures on Apple Watch Series 11, 42 mm and 46 mm. Preview data is an isolated design fixture, not your player save.');render();
</script></html>'''


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('manifest', type=Path)
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    study = json.loads(args.manifest.read_text(encoding='utf-8-sig'))
    if len(study['designs']) < 10:
        raise ValueError('The review requires at least ten distinct designs')
    recommendations = {
        '01': 'My preferred default: personal, clear and useful at a glance.',
        '02': 'The most game-like option, with an authored character animation and a small river scene.',
        '03': 'A focused health check using the last phone reading, rather than projected combat health.',
        '04': 'The yield-focused option. Switch between run totals and run-average rates.',
        '05': 'My preferred automatic view while the phone is away: gains, elapsed time and current distance.',
        '06': 'An optional Watch-local target. It does not create a quest or award anything in the game.',
        '07': 'A compact progress screen: six real skill levels without invented XP rings.',
        '08': 'The strongest Journal overview. Select a run, change the metric, then open its receipt.',
        '09': 'For comparing two recent outings. The labels retain their durations and estimated status.',
        '10': 'A map’s recent record. This is a sample of the retained runs, not a lifetime map archive.',
    }
    for design in study['designs']:
        design.setdefault('recommendation', recommendations.get(str(design['id']).zfill(2), ''))
        for size in ('42', '46'):
            for iteration in ('initial', 'final'):
                capture = design.get('captures', {}).get(size, {}).get(iteration)
                if not capture or not (args.manifest.parent / capture).is_file():
                    raise ValueError(f'Missing {size} mm {iteration} capture for {design["id"]}')
        for state in design.get('states', []):
            if not (args.manifest.parent / state['file']).is_file():
                raise ValueError(f'Missing state capture: {state["file"]}')
        for clip in design.get('clips') or ([design['clip']] if design.get('clip') else []):
            if not (args.manifest.parent / clip).is_file():
                raise ValueError(f'Missing native clip: {clip}')
    output = args.output or args.manifest.with_name('gallery.html')
    if output.parent != args.manifest.parent:
        raise ValueError('Keep gallery beside the manifest so media paths stay local')
    data = json.dumps(study, ensure_ascii=False).replace('</', r'<\/')
    output.write_text(PAGE.replace('__DATA__', data), encoding='utf-8')
    print(output.resolve())


if __name__ == '__main__':
    main()
