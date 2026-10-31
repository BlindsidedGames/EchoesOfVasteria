import AppKit
let args=CommandLine.arguments
let folder=args[1], size=args[2], revision=args[3]
let w: CGFloat=270, h: CGFloat=345
let cols: CGFloat=5
let canvas=NSImage(size:NSSize(width:cols*w,height:2*h))
canvas.lockFocus()
NSColor(calibratedRed:0.025,green:0.07,blue:0.08,alpha:1).setFill();NSRect(x:0,y:0,width:cols*w,height:2*h).fill()
let titles=["Expedition dial","Living trail","Combat watch","Harvest counter","Since you left","Run goal","Skillbook","Run ribbon","Run comparison","Map dossier"]
for i in 0..<10 {
 let image=NSImage(contentsOfFile:"\(folder)/\(String(format:"%02d",i+1))-\(size)-\(revision).png")!
 let origin=NSPoint(x:CGFloat(i%5)*w,y:CGFloat(1-i/5)*h)
 let rect=NSRect(x:origin.x+15,y:origin.y+35,width:w-30,height:h-60)
 image.draw(in:rect,from:.zero,operation:.sourceOver,fraction:1)
 let text="\(String(format:"%02d",i+1))  \(titles[i])"
 (text as NSString).draw(at:NSPoint(x:origin.x+12,y:origin.y+10),withAttributes:[.font:NSFont.systemFont(ofSize:14,weight:.medium),.foregroundColor:NSColor.white])
}
canvas.unlockFocus()
let rep=NSBitmapImageRep(data:canvas.tiffRepresentation!)!
try rep.representation(using:.png,properties:[:])!.write(to:URL(fileURLWithPath:"\(folder)/contact-\(size)-\(revision).png"))
