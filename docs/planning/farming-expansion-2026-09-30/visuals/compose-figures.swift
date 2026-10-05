import AppKit
import Foundation
let source = "/Users/matthewrushworth/Projects/Echoes Farming Planning Evidence/2026-09-30/renders/"
let output = "/Users/matthewrushworth/Projects/Echoes of Vasteria/docs/planning/farming-expansion-2026-09-30/visuals/"
func text(_ value:String,_ x:CGFloat,_ y:CGFloat,_ size:CGFloat=22,_ color:NSColor=NSColor.white) {
    let a:[NSAttributedString.Key:Any] = [.font:NSFont.systemFont(ofSize:size,weight:.medium),.foregroundColor:color]
    (value as NSString).draw(at:NSPoint(x:x,y:y),withAttributes:a)
}
func panel(_ file:String,_ title:String,_ subtitle:String,_ x:CGFloat,_ y:CGFloat,_ w:CGFloat,_ h:CGFloat) {
    text(title,x,y+h+37,24);text(subtitle,x,y+h+10,16,NSColor(calibratedWhite:0.78,alpha:1))
    NSImage(contentsOfFile:source+file)!.draw(in:NSRect(x:x,y:y,width:w,height:h),from:.zero,operation:.sourceOver,fraction:1)
}
func figure(_ name:String,_ w:Int,_ h:Int,_ draw:()->Void) {
    let rep=NSBitmapImageRep(bitmapDataPlanes:nil,pixelsWide:w,pixelsHigh:h,bitsPerSample:8,samplesPerPixel:4,hasAlpha:true,isPlanar:false,colorSpaceName:.deviceRGB,bytesPerRow:0,bitsPerPixel:0)!
    NSGraphicsContext.saveGraphicsState();NSGraphicsContext.current=NSGraphicsContext(bitmapImageRep:rep)
    NSColor(calibratedRed:0.08,green:0.10,blue:0.09,alpha:1).setFill();NSRect(x:0,y:0,width:w,height:h).fill();draw()
    NSGraphicsContext.restoreGraphicsState();try! rep.representation(using:.png,properties:[:])!.write(to:URL(fileURLWithPath:output+name+".png"))
}
figure("farm-progression",2460,1310) {
    text("Farm expansion: actual town, proposed stages",30,1260,31)
    text("Unity Edit-mode renders. Fixed wide camera. Southeast is lower right. Main and player progression are unchanged.",30,1227,19)
    let labels=[("A0-existing","0 | Authored farm and woodland","Current art/layout reference; not a fresh-save screenshot"),("A1-first-beds","1 | Prepare the existing fence area","Two beds, illustrated with different growth states"),("A2-stump-reclamation","2 | Reclaim the old stump field","Four beds; selected existing stumps cleared"),("A2b-southeast-work","2b | Cut the southeast edge","Standing trees become stumps; no new harvest batch"),("A3-southeast-clearing","3 | Finish the woodland field","Six beds; most surrounding woodland retained")]
    for (i,l) in labels.enumerated() {let col=i%3;let row=i/3;panel(l.0+"-town.png",l.1,l.2,CGFloat(30+col*810),CGFloat(row==0 ? 680:135),780,439)}
    text("Each bed is one finite batch, not nine separate inventory plots.",1650,510,19)
    text("Different crop stages demonstrate art reuse;",1650,475,19)
    text("they do not imply tutorial crop eligibility.",1650,447,19)
    text("Path edging, collision and yield are conceptual.",1650,398,19)
    text("Normal-scale close views are provided separately.",1650,349,19)
    text("Source scenes: Assets/Editor/FarmingPreviews",30,65,20)
    text("Planning only — no farming system implemented.",30,30,18)
}
figure("farm-layout-alternatives",2460,710) {
    text("Final layout alternatives in the wider town",30,660,31)
    text("Same camera and actual assets. Compare capacity, cleared canopy, retained trees and town composition.",30,625,19)
    panel("A3-southeast-clearing-town.png","A | Compact field — recommended","6 beds total; smaller southern enclosure",30,120,780,439)
    panel("B3-terraced-rows-town.png","B | Terraced rows","8 beds total; more clearing and visual density",840,120,780,439)
    panel("C3-grove-courtyard-town.png","C | Retained-tree courtyard","6 beds total; wider enclosure, one central tree retained",1650,120,780,439)
    text("Canopy bounds were included in preview clearing. Exact tree/stump changes are recorded in preview-manifest.json.",30,60,19)
    text("A dedicated farm focus is needed to show the southern expansion at the existing orthographic size18.",30,28,19)
}
figure("farm-normal-scale",2620,1700) {
    text("At the game's camera scale: north and southeast views",30,1650,31)
    text("Orthographic size18, 1280×720 per panel. Pan changes only; no zoom enlargement. UI omitted in Edit-mode renders.",30,1615,19)
    panel("A3-southeast-clearing-north-normal.png","Northern farm and reclaimed field","Existing windmill, farmers, town path and first four beds",30,825,1280,720)
    panel("A3-southeast-clearing-south-normal.png","A | Compact southern field","New track, opening, two batches and surrounding woodland",1330,825,1280,720)
    panel("B3-terraced-rows-south-normal.png","B | Terraced southern field","Four batches in the final enclosure; eight beds total",30,30,1280,720)
    panel("C3-grove-courtyard-south-normal.png","C | Courtyard southern field","Two batches flank a retained tree; larger open enclosure",1330,30,1280,720)
}
figure("farm-town-context-annotated",1600,1140) {
    text("Where the farm fits: measured town geometry",25,1090,30)
    text("Actual assets in an isolated layout. E = right (+X); S = down (-Y).",25,1055,19)
    NSImage(contentsOfFile:source+"A3-southeast-clearing-town.png")!.draw(in:NSRect(x:0,y:140,width:1600,height:900),from:.zero,operation:.sourceOver,fraction:1)
    func box(_ x:CGFloat,_ y:CGFloat,_ w:CGFloat,_ h:CGFloat,_ label:String,_ color:NSColor) {
        color.setStroke();let p=NSBezierPath(rect:NSRect(x:x,y:y,width:w,height:h));p.lineWidth=3;p.stroke()
        NSColor(calibratedWhite:0.08,alpha:0.9).setFill();NSRect(x:x,y:y+h+3,width:220,height:28).fill();text(label,x+6,y+h+7,17,color)
    }
    box(884,772,141,99,"1 | Existing fence footprint",.systemYellow)
    box(884,576,169,98,"2 | Existing stump field",.systemOrange)
    box(898,322,141,85,"3 | Southeast woodland edge",.systemCyan)
    text("1  Two beds within the farmers' existing footprint; keep the windmill/NPC approach clear.",25,103,20)
    text("2  Reclaim stumps already present in Main; this is not newly felled forest.",25,70,20)
    text("3  Clear selected standing trees, then add the last two beds; retain the surrounding canopy.",25,37,20)
}
figure("farm-clearing-close-comparison",2620,1750) {
    text("Southeast clearing: before, work in progress, finished field",30,1700,30)
    text("Actual standing-tree and stump sprites. Same camera (-61,-23), orthographic size18; no gameplay progression runs.",30,1665,19)
    panel("A2-stump-reclamation-south-normal.png","Before | Standing woodland edge","The nearby stump field has been reclaimed; the woodland remains",30,880,1280,720)
    panel("A2b-southeast-work-south-normal.png","During | Cut trees become stumps","Selected standing sprites change to their imported stump art",1330,880,1280,720)
    panel("A3-southeast-clearing-south-normal.png","After | Prepare the final two beds","Clear stumps, connect the path and fence a finite field",30,85,1280,720)
    text("A: 20 standing renderer objects changed.",1360,685,25)
    text("B and C: 31 standing renderer objects changed.",1360,635,25)
    text("All layouts reclaim 23 old stump renderer objects.",1360,585,25)
    text("These are visual counts, not authored log rewards.",1360,495,22)
    text("Most surrounding woodland is retained.",1360,445,22)
    text("The compact six-bed layout is recommended.",1360,395,22)
    text("Path palette/edges and navigation remain",1360,295,22)
    text("production validation work.",1360,260,22)
}
