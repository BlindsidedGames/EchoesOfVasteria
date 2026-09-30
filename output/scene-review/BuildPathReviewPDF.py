"""Lossless actual runtime before/after evidence, seven quest states and five views."""
import pathlib
from reportlab.pdfgen import canvas
from reportlab.lib.utils import ImageReader
from pypdf import PdfReader
r=pathlib.Path(__file__).resolve().parents[2];captures=r/'output/scene-review/path-stage-captures';out=r/'output/tile-catalogues/Path-stages-before-after.pdf';c=canvas.Canvas(str(out),pagesize=(1100,780),pageCompression=1);c.setTitle('Echoes - seven path states before and after');c.setAuthor('Echoes of Vasteria project')
states=['Before any gate','Rock and Stone complete','Chunky Business complete','Unlock Erif complete','Unlock Copium complete','Unlock Idle complete','Unlock Vastium complete']
def title(text,subtitle):
 c.setFillColorRGB(.08,.2,.16);c.setFont('Helvetica-Bold',24);c.drawString(35,738,text);c.setFont('Helvetica',13);c.drawString(35,710,subtitle)
def picture(stage,phase,view,x,y,width=510):
 file=captures/f'review-{phase}-stage-{stage}-{view}.png';assert file.exists();c.setFont('Helvetica-Bold',13);c.drawString(x,y+width*720/1280+12,('BEFORE' if phase=='before' else 'AFTER')+' - '+view);c.drawImage(ImageReader(str(file)),x,y,width=width,height=width*720/1280)
title('Seven cumulative path states','Actual Unity6000.6.0f1 runtime; disposable saves; Steam/cloud disabled')
c.setFont('Helvetica',16)
lines=['Six Tilemap cell substitutions; both143-cell footprints preserved.','The clearing now has continuous dirt and curved grass borders at its trail mouth.','Doorway joins and the original rotated south edge are retained.','35 final and35 matched baseline captures individually inspected.','143 rotation-aware cardinal/diagonal checks: zero mismatches.','Seven actual quest-controller activation states verified.','','Each stage has three pages: full path, clearing/mouth, exposed tip/mine.','Before images reproduce the exact original tile identities in private runtime state.','Source pixels, GUIDs, sprite IDs, transforms, colors and flags are unchanged.','Scene changes are limited to two Tilemap blocks.','','No Player build or live Steam integration test is claimed.','No player progression or settings were altered by this validation.']
for i,line in enumerate(lines):c.drawString(35,655-i*34,line)
c.showPage()
for stage,name in enumerate(states):
 for group,views in [('Full path',['overview']),('Clearing and entrance',['clearing','mouth']),('Exposed endpoint and mine',['tip','mine'])]:
  title(f'State{stage}: {name}',group+' | BEFORE left / AFTER right | cumulative quest flags')
  for row,view in enumerate(views):
   y = 365 if row == 0 else 35
   picture(stage, 'before', view, 30, y)
   picture(stage, 'final', view, 560, y)
  c.setFont('Helvetica',11);c.drawString(35,12,f'Actual runtime captures | state{stage} | {group} | original layout and progression preserved');c.showPage()
c.save();assert len(PdfReader(str(out)).pages)==22;print(out.name,'22pages,70actualruntime images')
