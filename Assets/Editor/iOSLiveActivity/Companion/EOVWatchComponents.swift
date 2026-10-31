import SwiftUI
import UIKit

enum EOVWatchPalette {
    static let ink = Color(red: 0.025, green: 0.07, blue: 0.08)
    static let panel = Color(red: 0.06, green: 0.14, blue: 0.15)
    static let gold = Color(red: 0.95, green: 0.76, blue: 0.43)
    static let teal = Color(red: 0.32, green: 0.78, blue: 0.68)
    static let cream = Color(red: 0.97, green: 0.93, blue: 0.84)
    static let muted = Color(red: 0.62, green: 0.72, blue: 0.69)
    static let red = Color(red: 0.94, green: 0.47, blue: 0.41)
}
struct EOVWatchScaledFont: ViewModifier {
    @ScaledMetric(relativeTo: .body) var points: CGFloat = 12
    let weight: Font.Weight
    let design: Font.Design
    init(_ size: CGFloat, weight: Font.Weight, design: Font.Design) {
        _points = ScaledMetric(wrappedValue: size, relativeTo: .body)
        self.weight = weight; self.design = design
    }
    func body(content: Content) -> some View { content.font(.system(size: points, weight: weight, design: design)) }
}
extension View {
    func eovWatchFont(_ size: CGFloat, weight: Font.Weight = .regular, design: Font.Design = .default) -> some View {
        modifier(EOVWatchScaledFont(size, weight: weight, design: design))
    }
}
struct EOVWatchGlyph: View {
    let name: String
    var size: CGFloat = 20
    var body: some View {
        Group {
            if name == "Heart" { Image(systemName: "heart.fill").resizable().scaledToFit().foregroundStyle(EOVWatchPalette.red) }
            else if let image = UIImage(named: name) { Image(uiImage: image).resizable().interpolation(.none).scaledToFit() }
            else { Image(systemName: name.hasPrefix("EOVSkill_") ? "sparkles" : "map.fill").resizable().scaledToFit().foregroundStyle(EOVWatchPalette.gold) }
        }.frame(width: size, height: size).accessibilityHidden(true)
    }
}
private final class EOVWatchPortraitFrames: NSObject {
    let frames: [[UIImage]]
    let bounds: CGRect
    private static let cache: NSCache<NSString, EOVWatchPortraitFrames> = {
        let cache = NSCache<NSString, EOVWatchPortraitFrames>(); cache.countLimit = 8; return cache
    }()
    static func load(_ names: [String]?) -> EOVWatchPortraitFrames? {
        guard let names, !names.isEmpty else { return nil }
        let key = names.joined(separator: "|") as NSString
        if let saved = cache.object(forKey: key) { return saved }
        var frames = [[UIImage]](), union = CGRect.null
        for frame in 0..<6 {
            var layers = [UIImage]()
            for name in names {
                guard let base = UIImage(named: name) else { return nil }
                let image = UIImage(named: name + "_WatchIdle" + String(frame)) ?? base
                layers.append(image)
                guard let cgImage = image.cgImage else { continue }
                var pixels = [UInt8](repeating: 0, count: 64 * 64 * 4)
                pixels.withUnsafeMutableBytes { bytes in
                    guard let context = CGContext(data: bytes.baseAddress, width: 64, height: 64,
                        bitsPerComponent: 8, bytesPerRow: 64 * 4, space: CGColorSpaceCreateDeviceRGB(),
                        bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue) else { return }
                    context.interpolationQuality = .none
                    context.draw(cgImage, in: CGRect(x: 0, y: 0, width: 64, height: 64))
                }
                for y in 0..<64 { for x in 0..<64 where pixels[(y * 64 + x) * 4 + 3] > 0 {
                    union = union.union(CGRect(x: x, y: y, width: 1, height: 1))
                } }
            }
            frames.append(layers)
        }
        guard !union.isNull else { return nil }
        let result = EOVWatchPortraitFrames(frames: frames, bounds: union)
        cache.setObject(result, forKey: key); return result
    }
    private init(frames: [[UIImage]], bounds: CGRect) { self.frames = frames; self.bounds = bounds }
}
struct EOVWatchHero: View {
    let presentation: EOVWatchPresentation
    var size: CGFloat = 44
    var animate = true
    @Environment(\.scenePhase) private var phase
    @Environment(\.isLuminanceReduced) private var dimmed
    @Environment(\.accessibilityReduceMotion) private var reduceMotion
    var body: some View {
        let portrait = EOVWatchPortraitFrames.load(presentation.snapshot?.portraitLayers)
        let enabled = animate && portrait != nil && presentation.heroCanAnimate && phase == .active && !dimmed && !reduceMotion
        TimelineView(.animation(minimumInterval: 1.0 / 8, paused: !enabled)) { context in
            let frame = enabled ? Int(context.date.timeIntervalSince1970 * 8) % 6 : 0
            if let portrait {
                let scale = size * 0.95 / max(portrait.bounds.width, portrait.bounds.height)
                ZStack {
                    ForEach(Array(portrait.frames[frame].enumerated()), id: \.offset) { _, image in
                        Image(uiImage: image).resizable().interpolation(.none).scaledToFit()
                            .frame(width: 64 * scale, height: 64 * scale)
                    }
                }.offset(x: (32 - portrait.bounds.midX) * scale, y: (32 - portrait.bounds.midY) * scale)
                    .frame(width: size, height: size).clipped()
            } else { EOVWatchGlyph(name: "EOVHero", size: size) }
        }.accessibilityElement(children: .ignore).accessibilityLabel(portrait == nil ? "Character preview" : "Your saved character")
    }
}
struct EOVWatchPanel<Content: View>: View {
    let title: String
    let presentation: EOVWatchPresentation
    let refresh: () -> Void
    var estimated = false
    var showSourceAge = true
    @ViewBuilder var content: Content
    @Environment(\.dynamicTypeSize) private var textSize
    var body: some View {
        GeometryReader { geo in
            VStack(spacing: 3) {
                HStack(spacing: 2) {
                    VStack(alignment: .leading, spacing: 1) {
                        Text(title).eovWatchFont(19, weight: .bold, design: .rounded).lineLimit(1).minimumScaleFactor(0.75)
                        if showSourceAge || presentation.cached || !presentation.reachable || presentation.age > 45 || presentation.refreshState != .idle || presentation.snapshot?.demo == true {
                            Text(presentation.source(estimated: estimated)).eovWatchFont(10).foregroundStyle(EOVWatchPalette.muted)
                                .lineLimit(1).accessibilityLabel(sourceDescription)
                        }
                    }.frame(maxWidth: .infinity, alignment: .leading)
                    Button(action: refresh) {
                        Group {
                            if presentation.refreshState == .requesting { ProgressView().tint(EOVWatchPalette.gold) }
                            else { Image(systemName: "arrow.clockwise").eovWatchFont(16, weight: .semibold) }
                        }.frame(width: 44, height: 44).contentShape(Rectangle())
                    }.buttonStyle(.plain).foregroundStyle(EOVWatchPalette.gold)
                        .disabled(presentation.refreshState == .requesting)
                        .accessibilityLabel("Refresh from iPhone")
                        .handGestureShortcut(.primaryAction, isEnabled: !textSize.isAccessibilitySize)
                }.frame(height: textSize.isAccessibilitySize ? 64 : 44)
                ScrollView {
                    content.frame(maxWidth: .infinity, minHeight: max(0, geo.size.height - (textSize.isAccessibilitySize ? 108 : 88)), alignment: .top).padding(.bottom, 3)
                }.scrollIndicators(.hidden).scrollInputBehavior(.enabled, for: .handGestureShortcut)
            }.padding(.horizontal, 7).padding(.top, 32).padding(.bottom, 6)
                .frame(width: geo.size.width, height: geo.size.height, alignment: .top)
                .foregroundStyle(EOVWatchPalette.cream)
        }.ignoresSafeArea()
    }
    private var sourceDescription: String {
        let prefix = estimated ? "Managed away estimate. " : "Phone snapshot. "
        return prefix + "Snapshot age " + presentation.ageLabel + ". " + presentation.refreshDescription
    }
}
struct EOVWatchMetric: View {
    let art: String
    let value: String
    let label: String
    var big = false
    var stacked = false
    var body: some View {
        Group {
            if stacked {
                VStack(spacing: 1) { EOVWatchGlyph(name: art, size: 13); Text(value).eovWatchFont(12, weight: .semibold, design: .rounded).monospacedDigit().lineLimit(1) }
            } else {
                HStack(spacing: 3) {
                    EOVWatchGlyph(name: art, size: big ? 23 : 16)
                    Text(value).eovWatchFont(big ? 25 : 13, weight: .semibold, design: .rounded).monospacedDigit().lineLimit(1).minimumScaleFactor(0.75)
                }
            }
        }.frame(maxWidth: .infinity).accessibilityElement(children: .ignore).accessibilityLabel(label).accessibilityValue(value)
    }
}
struct EOVWatchGain: View {
    let art: String
    let value: String
    let label: String
    var body: some View {
        HStack(spacing: 5) {
            EOVWatchGlyph(name: art, size: 18)
            Text(value).eovWatchFont(21, weight: .bold, design: .rounded).monospacedDigit().lineLimit(1).minimumScaleFactor(0.8)
        }.frame(maxWidth: .infinity, minHeight: 34).background(EOVWatchPalette.panel, in: RoundedRectangle(cornerRadius: 10))
            .accessibilityElement(children: .ignore).accessibilityLabel(label).accessibilityValue(value)
    }
}
struct EOVWatchRing: View {
    let fraction: Double?
    let active: Bool
    var body: some View {
        let color = active ? EOVWatchPalette.gold : EOVWatchPalette.muted
        ZStack {
            if let fraction {
                Circle().stroke(color.opacity(0.16), lineWidth: 7)
                if fraction > 0 { Circle().trim(from: 0, to: min(1, fraction)).stroke(color, style: StrokeStyle(lineWidth: 7, lineCap: .round)).rotationEffect(.degrees(-90)) }
            } else { RoundedRectangle(cornerRadius: 18).stroke(EOVWatchPalette.muted.opacity(0.2), lineWidth: 1) }
        }
    }
}
struct EOVWatchDistance: View {
    let distance: Double
    let cap: Double
    let reaping: Bool
    var body: some View {
        HStack(alignment: .firstTextBaseline, spacing: 3) {
            Text(eovWatchNumber(distance)).eovWatchFont(25, weight: .bold, design: .rounded)
            Text(reaping && cap > 0 ? "/ " + eovWatchNumber(cap) + " m" : "m").eovWatchFont(12).foregroundStyle(EOVWatchPalette.muted)
        }.monospacedDigit().lineLimit(1).minimumScaleFactor(0.75)
            .accessibilityLabel("Distance " + eovWatchNumber(distance, compact: false) + " metres" + (reaping && cap > 0 ? " of " + eovWatchNumber(cap, compact: false) : ", no distance reap"))
    }
}
func eovWatchOutcome(_ run: EOVWatchSnapshot.RecentRun) -> String { run.result == "reaped" ? "EOVCarl" : run.result == "died" ? "Heart" : "EOVHero" }
func eovWatchMapArt(_ id: String) -> String { "EOVMap_" + id }

struct EOVWatchQuartet: View {
    let run: EOVWatchDisplay
    var body: some View {
        let stacked = max(run.tasks ?? 0, run.kills ?? 0, run.resources) >= 10000
        HStack(spacing: 1) {
            EOVWatchMetric(art: "EOVTask", value: (run.estimated && run.tasks != nil ? "+" : "") + eovWatchNumber(run.tasks), label: run.estimated ? "Tasks gained while away" : "Phone run tasks", stacked: stacked)
            EOVWatchMetric(art: "EOVKill", value: (run.estimated && run.kills != nil ? "+" : "") + eovWatchNumber(run.kills), label: run.estimated ? "Kills gained while away" : "Phone run kills", stacked: stacked)
            EOVWatchMetric(art: "EOVChest", value: (run.estimated ? "+" : "") + eovWatchNumber(run.resources), label: run.estimated ? "Resources gained while away" : "Phone run resources", stacked: stacked)
            EOVWatchMetric(art: "EOVCarl", value: run.reaps.map(String.init) ?? "—", label: "Reaps while away; unavailable for a foreground phone snapshot", stacked: stacked)
        }.frame(height: stacked ? 32 : 25)
    }
}
func eovWatchMetres(_ value: Double) -> String { eovWatchNumber(value, compact: false) + " m" }
func eovWatchSkillArt(_ id: String) -> String { "EOVSkill_" + id }
