import Foundation
import SwiftUI

// This target never creates WCSession, loads a production cache, or awards progress.
// All fixture transformations are documented in the gallery, outside the game UI.
@MainActor
final class LabStore: ObservableObject {
    let design: Int
    let variant: String
    let largeText: Bool
    let clock: Double
    @Published var snapshot: EOVWatchSnapshot?
    @Published var refreshing = false
    @Published var refreshCount = 0
    @Published var animationStart = Date()

    init() {
        func argument(_ name: String) -> String? {
            ProcessInfo.processInfo.arguments.first(where: { $0.hasPrefix("--" + name + "=") })?
                .split(separator: "=", maxSplits: 1).last.map(String.init)
        }
        design = Int(argument("design") ?? "1") ?? 1
        variant = argument("state") ?? (design == 5 ? "away" : design >= 8 ? "history" : "live")
        largeText = argument("text") == "large"
        let file = variant == "empty" ? "live" : variant
        let url = Bundle.main.url(forResource: file, withExtension: "json", subdirectory: "Fixtures") ??
            Bundle.main.url(forResource: file, withExtension: "json")
        let decoded = url.flatMap { try? Data(contentsOf: $0) }.flatMap { try? EOVWatchSnapshot.decode($0) }
        snapshot = variant == "empty" ? nil : decoded
        let origin = decoded?.timeline?.generatedAt ?? decoded?.generatedAt ?? 0
        clock = Double(argument("clock") ?? "") ?? origin + (variant == "stale" ? 7200 : variant == "restart" ? 110 : ["away", "large-away"].contains(variant) ? 85 : 15)
    }
    var runStatus: String? { snapshot?.run?.dead == true ? "Died" : snapshot?.run?.reaped == true ? "Reaped" : nil }
    var display: RunDisplay { RunDisplay(snapshot, at: clock) }
    var offline: Bool { variant == "offline" || variant == "stale" || variant == "empty" }
    var canAnimate: Bool { !offline && snapshot?.phoneInBackground != true && display.active }
    var source: String {
        guard let s = snapshot else { return "Open Echoes on iPhone" }
        if refreshing { return "Asking iPhone…" }
        if offline { return variant == "stale" ? "Estimate ended · cached" : "iPhone unavailable · cached" }
        if display.expired { return "Estimate ended" }
        if display.phase == 1 { return "Restarting · estimated" }
        if display.phase == 2 { return "Return to iPhone" }
        return s.phoneInBackground == true ? "Estimated · " + elapsed + " away" : "iPhone snapshot · " + age + " ago"
    }
    var age: String { duration(max(0, clock - (snapshot?.generatedAt ?? clock))) }
    var elapsed: String { duration(max(0, clock - (snapshot?.timeline?.generatedAt ?? clock))) }
    func refresh() {
        guard !refreshing else { return }
        refreshing = true
        Task { @MainActor in
            try? await Task.sleep(for: .milliseconds(450))
            refreshing = false
            refreshCount += 1
            animationStart = .now
        }
    }
    var history: [EOVWatchSnapshot.RecentRun] { snapshot?.recentRuns.sorted { $0.runNumber > $1.runNumber } ?? [] }
}

struct RunDisplay {
    var map = "Echoes"
    var distance: Double = 0
    var cap: Double = 0
    var resources: Double = 0
    var tasks: Double?
    var kills: Double?
    var reaps: Int?
    var health: Double = 0
    var seconds: Double = 0
    var estimated = false
    var reaping = true
    var phase = 0
    var expired = false
    var active = false
    var fraction: Double? { reaping && cap > 0 ? min(1, max(0, distance / cap)) : nil }
    init(_ snapshot: EOVWatchSnapshot?, at unix: Double, allowEstimate: Bool = true) {
        guard let s = snapshot, let run = s.run, s.state == "run" else { return }
        map = run.mapTitle; distance = run.distance; cap = run.cap; resources = run.resources
        tasks = Double(run.tasks); kills = Double(run.kills); health = run.healthFraction; seconds = run.durationSeconds
        reaping = run.distanceReaping != false; active = !run.dead && !run.reaped
        if allowEstimate, active, s.phoneInBackground == true, s.backgroundProgressEnabled, let timeline = s.timeline,
           timeline.mapId == run.mapId {
            let sample = timeline.sample(at: unix)
            distance = sample.distance; cap = sample.cap; resources = sample.resources
            tasks = sample.tasks; kills = sample.kills; reaps = sample.reaps
            reaping = timeline.distanceReaping; phase = sample.phase; expired = sample.expired
            estimated = true; seconds = min(timeline.horizonSeconds, max(0, unix - timeline.generatedAt))
            active = !sample.expired && sample.phase != 2
        }
    }
}

func number(_ value: Double?, compact: Bool = true) -> String {
    guard let value else { return "—" }
    if compact {
        for (divisor, suffix) in [(1e9, "B"), (1e6, "M"), (1e3, "K")] where value >= divisor {
            return (value / divisor).formatted(.number.precision(.fractionLength(value / divisor >= 10 ? 0 : 1))) + suffix
        }
    }
    return value.rounded(.down).formatted(.number.precision(.fractionLength(0)))
}
func metres(_ value: Double) -> String { number(value, compact: false) + " m" }
func duration(_ value: Double) -> String {
    let s = Int(max(0, min(999999, value)))
    return s >= 3600 ? String(format: "%d:%02d:%02d", s / 3600, s / 60 % 60, s % 60) : String(format: "%d:%02d", s / 60, s % 60)
}
func rate(_ resources: Double, _ seconds: Double) -> String { seconds > 0 ? number(resources / seconds * 60) : "—" }
func skillArt(_ id: String) -> String { "Skill_" + id }
func outcome(_ run: EOVWatchSnapshot.RecentRun) -> String { run.result == "reaped" ? "EOVCarl" : run.result == "died" ? "Heart" : "EOVHero" }
func mapArt(_ id: String) -> String { ["River", "Woods", "Mines", "Farmlands", "Haloween"].contains(id) ? "Map_" + id : "MapFallback" }
