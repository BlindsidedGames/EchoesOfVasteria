import AppKit
let folder=CommandLine.arguments[1], size=CommandLine.arguments[2], revision=CommandLine.arguments[3]
let ids=["01","03","04","05","07","08","09"]
let names=["Expedition","Health","Resources","Since you left","Skillbook","Recent runs","Compare runs"]
let w:CGFloat=250,h:CGFloat=330
let canvas=NSImage(size:NSSize(width:4*w,height:2*h));canvas.lockFocus()
NSColor(calibratedRed:0.025,green:0.07,blue:0.08,alpha:1).setFill();NSRect(x:0,y:0,width:4*w,height:2*h).fill()
for i in 0..<ids.count {
 let image=NSImage(contentsOfFile:"\(folder)/\(ids[i])-\(size)-\(revision).png")!
 let x=CGFloat(i%4)*w,y=CGFloat(1-i/4)*h
 image.draw(in:NSRect(x:x+10,y:y+30,width:230,height:274),from:.zero,operation:.sourceOver,fraction:1)
 ("\(ids[i]) \(names[i])" as NSString).draw(at:NSPoint(x:x+10,y:y+8),withAttributes:[.font:NSFont.systemFont(ofSize:13,weight:.medium),.foregroundColor:NSColor.white])
}
canvas.unlockFocus();let rep=NSBitmapImageRep(data:canvas.tiffRepresentation!)!
try rep.representation(using:.png,properties:[:])!.write(to:URL(fileURLWithPath:"\(folder)/contact-\(size)-\(revision).png"))
