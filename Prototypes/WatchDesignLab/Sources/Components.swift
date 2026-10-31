import SwiftUI

enum Palette {
    static let ink = Color(red: 0.025, green: 0.07, blue: 0.08)
    static let panel = Color(red: 0.06, green: 0.14, blue: 0.15)
    static let gold = Color(red: 0.95, green: 0.76, blue: 0.43)
    static let teal = Color(red: 0.32, green: 0.78, blue: 0.68)
    static let cream = Color(red: 0.97, green: 0.93, blue: 0.84)
    static let muted = Color(red: 0.62, green: 0.72, blue: 0.69)
    static let red = Color(red: 0.94, green: 0.47, blue: 0.41)
}

struct Pixel: View {
    let name: String
    var size: CGFloat = 20
    var body: some View {
        Group {
            if name == "Heart" { Image(systemName: "heart.fill").foregroundStyle(Palette.red) }
            else { Image(name).resizable().interpolation(.none).scaledToFit() }
        }.frame(width: size, height: size).accessibilityHidden(true)
    }
}
struct Hero: View {
    var size: CGFloat = 50
    var running = false
    var animate = true
    @Environment(\.scenePhase) var phase
    @Environment(\.isLuminanceReduced) var dimmed
    @Environment(\.accessibilityReduceMotion) var reduceMotion
    var body: some View {
        let enabled = animate && phase == .active && !dimmed && !reduceMotion
        TimelineView(.animation(minimumInterval: 1.0 / 8, paused: !enabled)) { context in
            let frame = enabled ? Int(context.date.timeIntervalSince1970 * 8) % 6 : 0
            Image((running ? "HeroRun_" : "HeroIdle_") + String(frame))
                .resizable().interpolation(.none).scaledToFit()
                .frame(width: size * 2, height: size * 2)
                .frame(width: size, height: size).clipped()
        }.accessibilityLabel("Your saved character wearing a gold helmet and blue shirt")
    }
}
struct CarlArrival: View {
    @EnvironmentObject var lab: LabStore
    @Environment(\.scenePhase) var phase
    @Environment(\.isLuminanceReduced) var dimmed
    @Environment(\.accessibilityReduceMotion) var reduceMotion
    var size: CGFloat = 30
    var body: some View {
        let enabled = lab.canAnimate && phase == .active && !dimmed && !reduceMotion
        TimelineView(.explicit(enabled ? (0...3).map { lab.animationStart.addingTimeInterval(Double($0) / 12) } : [lab.animationStart])) { context in
            let elapsed = max(0, context.date.timeIntervalSince(lab.animationStart))
            let frame = enabled ? min(3, Int(elapsed * 12)) : 3
            Pixel(name: "CarlFly_" + String(frame), size: size)
        }
    }
}
struct LabHeader: View {
    let title: String
    @EnvironmentObject var lab: LabStore
    private var shortSource: String {
        if lab.refreshing { return "Asking iPhone…" }
        if lab.offline { return lab.variant == "stale" ? "Estimate ended" : "Offline · cached" }
        if lab.display.expired { return "Estimate ended" }
        if lab.display.phase == 1 { return "Restarting · est." }
        if lab.display.phase == 2 { return "Return to iPhone" }
        return lab.display.estimated ? "Estimated · " + lab.elapsed : "Snapshot · " + lab.age + " ago"
    }
    var body: some View {
        HStack(spacing: 2) {
            VStack(alignment: .leading, spacing: 1) {
                Text(title).font(.system(size: 19, weight: .bold, design: .rounded)).foregroundStyle(Palette.cream)
                    .lineLimit(1).minimumScaleFactor(0.7)
                Text(shortSource).font(.system(size: 10)).foregroundStyle(Palette.muted)
                    .lineLimit(1).minimumScaleFactor(0.8).accessibilityLabel(lab.source)
            }.frame(maxWidth: .infinity, alignment: .leading)
            Button { lab.refresh() } label: {
                Image(systemName: "arrow.clockwise").font(.system(size: 16, weight: .semibold))
                    .foregroundStyle(Palette.gold).frame(width: 44, height: 44).contentShape(Rectangle())
            }.buttonStyle(.plain).accessibilityLabel("Refresh iPhone snapshot")
        }.frame(height: 44)
    }
}
struct Screen<Content: View>: View {
    let title: String
    @ViewBuilder var content: Content
    var body: some View {
        VStack(spacing: 5) {
            LabHeader(title: title)
            content.frame(maxWidth: .infinity, maxHeight: .infinity)
        }.padding(.horizontal, 5).padding(.bottom, 5).foregroundStyle(Palette.cream)
    }
}
struct Metric: View {
    let art: String
    let value: String
    let label: String
    var big = false
    var body: some View {
        HStack(spacing: 3) {
            Pixel(name: art, size: big ? 23 : 16)
            Text(value).font(.system(size: big ? 25 : 13, weight: .semibold, design: .rounded))
                .monospacedDigit().lineLimit(1).minimumScaleFactor(0.65)
        }.frame(maxWidth: .infinity, alignment: .center)
            .accessibilityElement(children: .ignore).accessibilityLabel(label).accessibilityValue(value)
    }
}
struct Quartet: View {
    let run: RunDisplay
    var body: some View {
        HStack(spacing: 1) {
            Metric(art: "EOVTask", value: number(run.tasks), label: "Tasks")
            Metric(art: "EOVKill", value: number(run.kills), label: "Kills")
            Metric(art: "EOVChest", value: (run.estimated ? "+" : "") + number(run.resources), label: run.estimated ? "Resources gained since departure" : "Run resources")
            Metric(art: "EOVCarl", value: run.reaps.map(String.init) ?? "—", label: "Reaps since departure")
        }.frame(height: 25)
    }
}
struct DistanceLine: View {
    let run: RunDisplay
    var body: some View {
        HStack(alignment: .firstTextBaseline, spacing: 3) {
            Text(number(run.distance)).font(.system(size: 25, weight: .bold, design: .rounded))
            Text(run.reaping && run.cap > 0 ? "/ " + number(run.cap) + " m" : "m")
                .font(.system(size: 12)).foregroundStyle(Palette.muted)
        }.monospacedDigit().lineLimit(1).minimumScaleFactor(0.7)
            .accessibilityLabel(run.reaping ? "Distance " + metres(run.distance) + " of " + metres(run.cap) : "Distance " + metres(run.distance) + ", no distance reap")
    }
}
struct Ring: View {
    let fraction: Double?
    var color = Palette.gold
    var width: CGFloat = 7
    var body: some View {
        ZStack {
            if let fraction {
                Circle().stroke(color.opacity(0.16), lineWidth: width)
                Circle().trim(from: 0, to: max(0.001, min(1, fraction)))
                    .stroke(color, style: StrokeStyle(lineWidth: width, lineCap: .round))
                    .rotationEffect(.degrees(-90))
            } else { RoundedRectangle(cornerRadius: 18).stroke(Palette.muted.opacity(0.2), lineWidth: 1) }
        }
    }
}
struct ValueTile: View {
    let art: String
    let value: String
    let label: String
    var body: some View {
        VStack(spacing: 2) {
            Pixel(name: art, size: 24)
            Text(value).font(.system(size: 24, weight: .bold, design: .rounded)).monospacedDigit()
                .lineLimit(1).minimumScaleFactor(0.65)
        }.frame(maxWidth: .infinity, minHeight: 62)
            .background(Palette.panel, in: RoundedRectangle(cornerRadius: 12))
            .accessibilityElement(children: .ignore).accessibilityLabel(label).accessibilityValue(value)
    }
}
struct EmptyExpedition: View {
    @EnvironmentObject var lab: LabStore
    var body: some View {
        Screen(title: "Echoes") {
            VStack(spacing: 9) {
                Pixel(name: "EOVHero", size: 42)
                Text(lab.snapshot?.state == "town" ? "Between adventures" : "No snapshot yet").font(.headline)
                Text("Open Echoes on iPhone").font(.caption2).foregroundStyle(Palette.muted)
                Button("Try again") { lab.refresh() }.buttonStyle(.bordered).frame(minHeight: 44)
            }
        }
    }
}
struct RunDetail: View {
    let run: EOVWatchSnapshot.RecentRun
    var body: some View {
        ScrollView {
            VStack(spacing: 9) {
                Pixel(name: outcome(run), size: 38)
                Text(run.mapTitle).font(.title3.bold())
                Text((run.estimated ? "Estimated · " : "Recorded · ") + run.result.capitalized).font(.caption2).foregroundStyle(Palette.muted)
                Metric(art: "EOVChest", value: number(run.resources), label: "Resources", big: true)
                HStack { Text(metres(run.distance)); Spacer(); Text(duration(run.durationSeconds)) }.font(.caption)
                HStack { Metric(art: "EOVTask", value: number(Double(run.tasks)), label: "Tasks"); Metric(art: "EOVKill", value: number(Double(run.kills)), label: "Kills") }
                Text("Run average " + rate(run.resources, run.durationSeconds) + "/min").font(.caption2).foregroundStyle(Palette.muted)
            }.padding(8)
        }.navigationTitle("Run " + String(run.runNumber))
    }
}
