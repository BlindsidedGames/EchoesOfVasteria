import AppKit
import Foundation
let source = "/Users/matthewrushworth/Projects/Echoes Farming Planning Evidence/2026-09-30/revision-2/renders/"
let output = "/Users/matthewrushworth/Projects/Echoes of Vasteria/docs/planning/farming-expansion-2026-09-30/visuals/revision-2/"
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

figure("farm-progression",3260,3230) {
 text("Farm expansion | revised matching paths and cultivated beds",30,3175,38)
 text("Actual Unity previews. Crop types illustrate art; tutorial starts with Radish. No live gameplay changed.",30,3130,25)
 panel("A0-existing-north-close.png","0 | Current authored footprint","North view; windmill and farmers are the scale reference",30,2140,1600,900)
 panel("A1-first-beds-north-close.png","1 | Prepare two beds","South gate connects to the existing town lane",1660,2140,1600,900)
 panel("A2-stump-reclamation-north-close.png","2 | Reclaim the old stump field","Four beds total; the second field has a north gate",30,1100,1600,900)
 panel("A2b-southeast-work-south-close.png","2b | Work phase inside the third quest","Southern view; selected trees become stumps before building",1660,1100,1600,900)
 panel("A3-southeast-clearing-south-close.png","3 | Complete the compact southern field","Six beds total; branch the existing logging lane through its gate",30,60,1600,900)
 text("Stages 0, 1 and 2 use the same north camera.",1690,795,31)
 text("Stages 2b and 3 use the same south camera.",1690,745,31)
 text("20 of 28 local southeast standing renderers",1690,645,31)
 text("are cut in A; this is a substantial local clearing.",1690,598,31)
 text("Cultivated soil uses existing perimeter variants.",1690,498,31)
 text("Paths use the current brown town-track art.",1690,451,31)
 text("Concept only: no crop production or save writes.",1690,351,31)
}
figure("farm-layout-alternatives",1640,3210) {
 text("Southeast alternatives | matched enlarged views",20,3155,34)
 text("Same camera (-58,-27), size9. Open individual full-size views from visuals.md.",20,3110,23)
 panel("A3-southeast-clearing-south-close.png","A | Compact field — recommended","6 total beds; two here. Smaller footprint, 20 local standing renderers cut.",20,2110,1600,900)
 panel("B3-terraced-rows-south-close.png","B | Stacked rows on flat ground","8 total beds; four here. Deeper enclosure; 31 standing renderers cut.",20,1070,1600,900)
 panel("C3-grove-courtyard-south-close.png","C | Retained-tree courtyard","6 total beds; two here. Central tree retained, but 31 standing renderers cut.",20,30,1600,900)
}
figure("farm-clearing-close-comparison",3260,2170) {
 text("Southeast construction | before, cut, finished",30,2115,38)
 text("Identical enlarged camera. Existing tree/stump art; this work phase is part of build quest3, not a fourth unlock.",30,2070,25)
 panel("A2-stump-reclamation-south-close.png","Before | Standing woodland","Four productive beds elsewhere; no southern crops yet",30,1060,1600,900)
 panel("A2b-southeast-work-south-close.png","During | Felled trees become stumps","Persist the work phase only after its material hand-in commits",1660,1060,1600,900)
 panel("A3-southeast-clearing-south-close.png","After | Clear, fence and cultivate","Logging lane joins the open north gate; two finite batches",30,30,1600,900)
 text("A clears 20 of 28 local standing renderers.",1700,795,32)
 text("It preserves the forest boundary around the field.",1700,740,29)
 text("Old stump reclamation changes 23 renderer objects.",1700,640,29)
 text("Counts describe visual objects, not resource payouts.",1700,585,29)
 text("Navigation and UI need later gameplay validation.",1700,475,29)
}
figure("farm-normal-scale",2620,1700) {
 text("The revised farm at the game's normal camera scale",30,1650,34)
 text("Size18, 1280×720 panels. Existing TownCameraPan can provide the farm focus; gameplay UI is omitted.",30,1615,21)
 panel("A3-southeast-clearing-north-normal.png","Northern farm and reclaimed field","Cultivated beds, two real gates, clear windmill approach",30,825,1280,720)
 panel("A3-southeast-clearing-south-normal.png","A | Compact southern field","Matching brown track and an open north entrance",1330,825,1280,720)
 panel("B3-terraced-rows-south-normal.png","B | Stacked southern rows","Eight total beds; same normal camera scale",30,30,1280,720)
 panel("C3-grove-courtyard-south-normal.png","C | Courtyard around a tree","Six total beds; larger enclosure and clearing",1330,30,1280,720)
}
figure("farm-town-context-annotated",1600,1140) {
 text("Where the farm fits | revised paths and measured geometry",25,1090,30)
 text("Actual assets in an isolated layout. E = right (+X); S = down (-Y).",25,1055,21)
 NSImage(contentsOfFile:source+"A3-southeast-clearing-town.png")!.draw(in:NSRect(x:0,y:140,width:1600,height:900),from:.zero,operation:.sourceOver,fraction:1)
 func box(_ x:CGFloat,_ y:CGFloat,_ w:CGFloat,_ h:CGFloat,_ label:String,_ color:NSColor) {
  color.setStroke();let p=NSBezierPath(rect:NSRect(x:x,y:y,width:w,height:h));p.lineWidth=3;p.stroke()
  NSColor(calibratedWhite:0.08,alpha:0.95).setFill();NSRect(x:x,y:y+h+3,width:270,height:31).fill();text(label,x+6,y+h+7,18,color)
 }
 box(884,772,141,99,"1 | Original farmers' footprint",.systemYellow)
 box(884,576,169,98,"2 | Reclaimed stump field",.systemOrange)
 box(898,322,141,85,"3 | Southeast woodland field",.systemCyan)
 text("1  Two cultivated beds; preserve windmill and farmer access.",25,103,21)
 text("2  Two more beds on already felled ground; enter from the north.",25,70,21)
 text("3  Cut 20 of 28 local standing renderers; final two beds join the logging lane.",25,37,21)
}
