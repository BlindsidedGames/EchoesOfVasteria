import AppKit
import Foundation
let root="/Users/matthewrushworth/Projects/Echoes of Vasteria/"
let out=root+"docs/planning/farming-expansion-2026-09-30/visuals/revision-4/"
let sprites:[(String,String,NSRect)]=[("Generic mature growth","Crops/Fruit_Tree_Stages.png",NSRect(x:64,y:0,width:32,height:64)),("Standing fruit-tree art","Trees/Big_Fruit_Tree.png",NSRect(x:64,y:0,width:32,height:64)),("Fruiting Apple_Tree","Crops/Apple_Tree.png",NSRect(x:0,y:0,width:32,height:64))]
let rep=NSBitmapImageRep(bitmapDataPlanes:nil,pixelsWide:1280,pixelsHigh:760,bitsPerSample:8,samplesPerPixel:4,hasAlpha:true,isPlanar:false,colorSpaceName:.deviceRGB,bytesPerRow:0,bitsPerPixel:0)!;NSGraphicsContext.saveGraphicsState();NSGraphicsContext.current=NSGraphicsContext(bitmapImageRep:rep);NSGraphicsContext.current!.imageInterpolation = .none
NSColor(calibratedRed:0.08,green:0.10,blue:0.09,alpha:1).setFill();NSRect(x:0,y:0,width:1280,height:760).fill()
for(i,s) in sprites.enumerated(){let image=NSImage(contentsOfFile:root+"Assets/Art/Packs/Cute_Fantasy/"+s.1)!;image.draw(in:NSRect(x:40+i*420,y:95,width:256,height:512),from:s.2,operation:.sourceOver,fraction:1);(s.0 as NSString).draw(at:NSPoint(x:40+i*420,y:640),withAttributes:[.font:NSFont.systemFont(ofSize:24),.foregroundColor:NSColor.white])}
NSGraphicsContext.restoreGraphicsState();try!rep.representation(using:.png,properties:[:])!.write(to:URL(fileURLWithPath:out+"tree-world-art-comparison.png"))
