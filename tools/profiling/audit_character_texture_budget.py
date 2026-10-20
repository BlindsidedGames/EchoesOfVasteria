"""Read-only source budget for the wardrobe pulled in by a root Resources search.

PNG size is not runtime memory. RGBA figures are dimensional estimates for one
uncompressed copy, not a physical-footprint measurement or additive Unity counter.
"""
from pathlib import Path
import collections
import json
import re
import struct
import sys

root = Path(__file__).resolve().parents[2]
parts = list((root / 'Assets/Resources/Characters/Parts').glob('*.asset'))
references = set()
frame_references = 0
for part in parts:
    frames = part.read_text(encoding='utf-8-sig').split('  frames:', 1)[1]
    guids = re.findall(r'guid: ([0-9a-f]{32})', frames)
    references.update(guids)
    frame_references += len(guids)

textures = []
for png in (root / 'Assets/Art/Character/Modular').rglob('*.png'):
    meta = png.with_suffix('.png.meta').read_text(encoding='utf-8-sig')
    guid = re.search(r'^guid: (\w+)', meta, re.M)[1]
    if guid not in references:
        continue
    with png.open('rb') as source:
        header = source.read(24)
    width, height = struct.unpack('>II', header[16:24])
    platforms = {}
    for chunk in meta.split('    buildTarget: ')[1:]:
        name = chunk.splitlines()[0]
        fields = {}
        for key in ('maxTextureSize', 'textureCompression', 'textureFormat', 'overridden'):
            value = re.search(r'^    ' + key + r': (-?\d+)', chunk, re.M)
            if value:
                fields[key] = int(value[1])
        platforms[name] = fields
    textures.append(dict(path=str(png.relative_to(root)), guid=guid, width=width,
        height=height, pngBytes=png.stat().st_size, oneRGBA32CopyBytes=width*height*4,
        readable=int(re.search(r'^  isReadable: (\d+)', meta, re.M)[1]),
        mipmaps=int(re.search(r'^    enableMipMap: (\d+)', meta, re.M)[1]),
        platforms=platforms))

result = dict(partDefinitions=len(parts), spriteFrameReferences=frame_references,
    referencedTextureGuids=len(references), matchedModularTextures=len(textures),
    pngBytes=sum(t['pngBytes'] for t in textures),
    oneRGBA32CopyBytes=sum(t['oneRGBA32CopyBytes'] for t in textures),
    readableTextureCount=sum(t['readable'] for t in textures),
    mipmappedTextureCount=sum(t['mipmaps'] for t in textures),
    dimensions=dict(collections.Counter(f"{t['width']}x{t['height']}" for t in textures)),
    textures=textures,
    limits=['Source-reference and dimensional audit, not physical device attribution.',
        'PNG compression does not establish GPU texture format or residency.',
        'Actual imported format and measured runtime counters take precedence.',
        'No duplicated CPU texture copy is assumed for non-readable assets.'])
output = Path(sys.argv[1]) if len(sys.argv)>1 else None
if output:
    output.write_text(json.dumps(result, indent=2), encoding='utf-8')
print(json.dumps({k:v for k,v in result.items() if k!='textures'}, indent=2))
