import Foundation
import ImageIO
import CoreGraphics
import UniformTypeIdentifiers
struct Layer: Decodable { let path: String; let rect: [Int] }
struct Job: Decodable { let output: String; let layers: [Layer] }
let jobs = try JSONDecoder().decode([Job].self, from: Data(contentsOf: URL(fileURLWithPath: CommandLine.arguments[1])))
for job in jobs {
    var pieces: [CGImage] = []
    for layer in job.layers {
        let source = CGImageSourceCreateWithURL(URL(fileURLWithPath: layer.path) as CFURL, nil)!
        let image = CGImageSourceCreateImageAtIndex(source, 0, nil)!
        let r = layer.rect
        let crop = image.cropping(to: CGRect(x: r[0], y: image.height-r[1]-r[3], width:r[2], height:r[3]))!
        pieces.append(crop)
    }
    let w=pieces[0].width, h=pieces[0].height
    let ctx=CGContext(data:nil,width:w,height:h,bitsPerComponent:8,bytesPerRow:w*4,space:CGColorSpaceCreateDeviceRGB(),bitmapInfo:CGImageAlphaInfo.premultipliedLast.rawValue)!
    ctx.interpolationQuality = .none
    for piece in pieces { ctx.draw(piece,in:CGRect(x:0,y:0,width:w,height:h)) }
    let dest=CGImageDestinationCreateWithURL(URL(fileURLWithPath:job.output) as CFURL,UTType.png.identifier as CFString,1,nil)!
    CGImageDestinationAddImage(dest,ctx.makeImage()!,nil)
    precondition(CGImageDestinationFinalize(dest))
}
print("Rendered \(jobs.count) assets without resampling")
