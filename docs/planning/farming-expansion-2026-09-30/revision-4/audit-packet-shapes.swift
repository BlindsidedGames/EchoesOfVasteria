import AppKit
import Foundation
let out="/Users/matthewrushworth/Projects/Echoes Farming Planning Evidence/2026-09-30/revision-4/"
let sheets=try! JSONSerialization.jsonObject(with:Data(contentsOf:URL(fileURLWithPath:out+"seed-tree-art-inventory.json"))) as! [[String:Any]]
let sh=sheets.first{($0["path"] as! String).hasSuffix("/Crops/Crops.png")}!
let bitmap=NSBitmapImageRep(data:try!Data(contentsOf:URL(fileURLWithPath:"/Users/matthewrushworth/Projects/Echoes of Vasteria/"+(sh["path"] as! String))))!
var rows=[[String:Any]]()
for s in sh["sprites"] as! [[String:Any]] {
 let r=s["rect"] as! [String:Double];let w=Int(r["width"]!),h=Int(r["height"]!),rx=Int(r["x"]!),ry=Int(r["y"]!)
 var coords=[(Int,Int)]()
 for y in 0..<h{for x in 0..<w{if bitmap.colorAt(x:rx+x,y:bitmap.pixelsHigh-1-(ry+y))!.alphaComponent>0.5{coords.append((x,y))}}}
 let xmin=coords.map{$0.0}.min()!,xmax=coords.map{$0.0}.max()!,ymin=coords.map{$0.1}.min()!,ymax=coords.map{$0.1}.max()!
 let mask=(ymin...ymax).map{y in (xmin...xmax).map{x in coords.contains{$0.0==x&&$0.1==y} ? "1":"0"}.joined()}.joined(separator:"/")
 rows.append(["name":s["name"]!,"rect":r,"opaqueBounds":[xmin,ymin,xmax,ymax],"tightAlphaMask":mask])
}
let masks=Set(rows.map{$0["tightAlphaMask"] as! String})
let j:[String:Any]=["sprites":rows,"distinctTightAlphaMasks":masks.count,"sharedOpaqueSilhouette":masks.count==1]
try!JSONSerialization.data(withJSONObject:j,options:.prettyPrinted).write(to:URL(fileURLWithPath:out+"seed-packet-shapes.json"))
print("Audited \(rows.count) seed packets: \(masks.count) distinct tight alpha masks")
