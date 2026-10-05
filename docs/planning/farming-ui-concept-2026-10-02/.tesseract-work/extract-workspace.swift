import AppKit
import Foundation
let folder = CommandLine.arguments[1]
let jobs = try JSONSerialization.jsonObject(with: Data(contentsOf: URL(fileURLWithPath: folder + "/.tesseract-work/workspace-extractions.json"))) as! [[String:Any]]
for j in jobs {
    let b = NSBitmapImageRep(data: try Data(contentsOf: URL(fileURLWithPath: j["path"] as! String)))!
    let r = j["rect"] as! [String:Int], scale = j["scale"] as! Int
    var x0 = r["x"]!, y0 = b.pixelsHigh-r["y"]!-r["height"]!, w = r["width"]!, h = r["height"]!
    if j["tight"] as? Bool == true {
        var lx=w, ly=h, hx=0, hy=0
        for y in 0..<h { for x in 0..<w { var a=[UInt](repeating:0,count:4); b.getPixel(&a,atX:x0+x,y:y0+y); if a[3]>0 {lx=min(lx,x);ly=min(ly,y);hx=max(hx,x);hy=max(hy,y)} } }
        x0+=lx; y0+=ly; w=hx-lx+1; h=hy-ly+1
    }
    let dest = NSBitmapImageRep(bitmapDataPlanes:nil,pixelsWide:w*scale,pixelsHigh:h*scale,bitsPerSample:8,samplesPerPixel:4,hasAlpha:true,isPlanar:false,colorSpaceName:.deviceRGB,bytesPerRow:0,bitsPerPixel:0)!
    for y in 0..<h*scale {for x in 0..<w*scale {var a=[UInt](repeating:0,count:4);b.getPixel(&a,atX:x0+x/scale,y:y0+y/scale);if !b.hasAlpha {a[3]=255};dest.setPixel(&a,atX:x,y:y)}}
    try dest.representation(using:.png,properties:[:])!.write(to:URL(fileURLWithPath:folder+"/assets/"+(j["name"] as! String)+".png"))
}
print("Extracted native art without resampling or changing Unity assets")
