"""Package lossless labelled native-pixel pages as private per-pack review PDFs."""
import json,pathlib
from PIL import Image
from reportlab.pdfgen import canvas
from reportlab.lib.utils import ImageReader
ROOT=pathlib.Path(__file__).resolve().parents[2];OUT=ROOT/'output/tile-catalogues'
idx=json.loads((OUT/'index.json').read_text());manifest=[]
for pack in idx['packs']:
    dest=OUT/(pack['pack']+'-catalogue.pdf');c=canvas.Canvas(str(dest),pagesize=(900,620),pageCompression=1)
    c.setTitle('Echoes terrain catalogue - '+pack['pack']);c.setAuthor('Echoes of Vasteria project');c.setFont('Helvetica-Bold',27);c.drawString(42,565,pack['pack'])
    c.setFont('Helvetica',17);c.drawString(42,530,f"{pack['sprites']} imported variants | {pack['sources']} source sheets | {len(pack['pages'])} visual pages")
    lines=['Every imported variant is visually reviewed and keyed by Unity GUID + local ID.','Old and new names, source sheet and native pixel size appear beside each preview.','Small art is enlarged with nearest-neighbour pixels. Large assemblies have native 2x pages.','','Orientation: N up, E right, S down, W left; unrotated artwork.','GrassSide_N / WaterSide_N identify the material on the north.','DirtEdge_N is the north boundary of dirt, with grass at the north; approved convention retained.','Frame / Src suffixes retain identity. Serialized playback order and rules are unchanged.','Atlas / Assembly means a compound imported crop, not a resliced individual autotile.','? marks contextual uncertainty, detailed in the machine-readable mapping.','','Mapping and usage: docs/TerrainTiles/AllPacks/tiles.json and README.md','Duplicate identities are retained. Exact-pixel groups and lookalike candidates are separate.','Existing unresolved art on 47 tile objects is recorded; no missing artwork was invented.','','Private asset-owner review. Pack licences prohibit redistribution/resale of the artwork.','Modification rights are documented where local licence files exist; missing licences are flagged.']
    c.setFont('Helvetica',14);y=480
    for line in lines:c.drawString(42,y,line);y-=23
    c.showPage()
    for number,page in enumerate(pack['pages'],1):
        image=Image.open(OUT/page['file']);w,h=image.size;scale=.4;c.setPageSize((w*scale,h*scale));c.bookmarkPage('page'+str(number));c.addOutlineEntry(page['file'],'page'+str(number),level=0)
        c.drawImage(ImageReader(image),0,0,width=w*scale,height=h*scale);c.showPage();image.close()
    c.save();manifest.append({'pack':pack['pack'],'file':str(dest),'pages':len(pack['pages'])+1,'sprites':pack['sprites']});print(dest.name,flush=True)
(OUT/'pdf-manifest.json').write_text(json.dumps(manifest,indent=2))
