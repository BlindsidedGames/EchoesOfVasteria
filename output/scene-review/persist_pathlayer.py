from pathlib import Path
import re
p=Path;stem='output/scene-review/Main.'
before=p(stem+'before-pathlayer-disk.unity').read_text(encoding='utf-8-sig');live=p(stem+'before-pathlayer-live.unity').read_text(encoding='utf-8-sig');after=p(stem+'after-pathlayer-live.unity').read_text(encoding='utf-8-sig');pat=r'(?ms)^--- !u!\d+ &(-?\d+)[^\n]*\n.*?(?=^--- !u!|\Z)'
def rec(s):return {m[1]:m[0] for m in re.finditer(pat,s)}
b,l,a=rec(before),rec(live),rec(after);changed={i for i in a.keys()&l.keys() if a[i]!=l[i]};assert len(changed)==1;assert p('Assets/Scenes/Main.unity').read_text(encoding='utf-8-sig')==before
out=before[:before.index('--- !u!')]+''.join(a[i] if i in changed else v for i,v in b.items());p('Assets/Scenes/Main.unity').write_text(out,encoding='utf-8',newline='\n');print('Persisted one path tilemap')
