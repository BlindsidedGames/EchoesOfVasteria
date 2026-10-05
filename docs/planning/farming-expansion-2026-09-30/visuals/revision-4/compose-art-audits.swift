import AppKit
import Foundation
let root="/Users/matthewrushworth/Projects/Echoes of Vasteria/"
let out=root+"docs/planning/farming-expansion-2026-09-30/visuals/revision-4/"
let evidence="/Users/matthewrushworth/Projects/Echoes Farming Planning Evidence/2026-09-30/revision-4/"
func label(_ value:String,_ x:CGFloat,_ y:CGFloat,_ size:CGFloat=20){(value as NSString).draw(at:NSPoint(x:x,y:y),withAttributes:[.font:NSFont.systemFont(ofSize:size),.foregroundColor:NSColor.white])}
func canvas(_ name:String,_ w:Int,_ h:Int,_ draw:()->Void){let rep=NSBitmapImageRep(bitmapDataPlanes:nil,pixelsWide:w,pixelsHigh:h,bitsPerSample:8,samplesPerPixel:4,hasAlpha:true,isPlanar:false,colorSpaceName:.deviceRGB,bytesPerRow:0,bitsPerPixel:0)!;NSGraphicsContext.saveGraphicsState();NSGraphicsContext.current=NSGraphicsContext(bitmapImageRep:rep);NSGraphicsContext.current!.imageInterpolation = .none;NSColor(calibratedRed:0.08,green:0.10,blue:0.09,alpha:1).setFill();NSRect(x:0,y:0,width:w,height:h).fill();draw();NSGraphicsContext.restoreGraphicsState();try!rep.representation(using:.png,properties:[:])!.write(to:URL(fileURLWithPath:out+name+".png"))}
let data=try!JSONSerialization.jsonObject(with:Data(contentsOf:URL(fileURLWithPath:evidence+"seed-packet-shapes.json"))) as! [String:Any]
let packets=(data["sprites"] as! [[String:Any]]).sorted{($0["name"] as! String)<($1["name"] as! String)}
let crop=NSImage(contentsOfFile:root+"Assets/Art/Packs/Cute_Fantasy/Crops/Crops.png")!
canvas("seed-packet-art-audit",1440,1120){
 label("Existing packet artwork | same visible silhouette",20,1070,32)
 label("22 imported packet sprites; tight opaque shape12×15px is identical. Actual art, no replacement generated.",20,1028,21)
 for (i,p) in packets.enumerated(){let r=p["rect"] as! [String:Double];let b=p["opaqueBounds"] as! [Int];let x=CGFloat((i%6)*240+55),y=CGFloat(810-(i/6)*195);let w=b[2]-b[0]+1,h=b[3]-b[1]+1;crop.draw(in:NSRect(x:x,y:y,width:CGFloat(w*8),height:CGFloat(h*8)),from:NSRect(x:r["x"]!+Double(b[0]),y:r["y"]!+Double(b[1]),width:Double(w),height:Double(h)),operation:.sourceOver,fraction:1);label((p["name"] as! String).replacingOccurrences(of:"Crop_",with:"").replacingOccurrences(of:"_SeedPacket",with:""),CGFloat((i%6)*240+20),y-28,18)}
 let core=NSImage(contentsOfFile:root+"Assets/Art/Packs/Cute_Fantasy_UI/Cores.png")!;core.draw(in:NSRect(x:1070,y:38,width:112,height:112),from:NSRect(x:128,y:0,width:16,height:16),operation:.sourceOver,fraction:1)
 label("Existing shared unknown core",1000,15,16)
 label("Inventory: one authored UnknownIcon; journal: hidden image + ?.",20,155,21)
 label("Recommended gap: one common unknown packet, matching the core silhouette convention.",20,110,21)
 label("21 packet canvases16×32; Wheat16×16. Normalize UI without reslicing referenced assets.",20,65,20)
}
canvas("fruit-object-art-audit",1000,260){label("Existing loose fruit objects | species not assigned",20,220,26);let im=NSImage(contentsOfFile:root+"Assets/Art/Packs/Cute_Fantasy/Crops/Fruit_Trees_Fruit_Objects.png")!;for i in 0..<5{im.draw(in:NSRect(x:CGFloat(20+i*195),y:40,width:128,height:128),from:NSRect(x:CGFloat(i*16),y:0,width:16,height:16),operation:.sourceOver,fraction:1);label("Objects_\(i)",CGFloat(20+i*195),10,18)}}
