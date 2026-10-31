import Foundation

enum EOVWatchRefreshState: Equatable { case idle, requesting, queued, unavailable, rejected }
enum EOVWatchScreen: String, CaseIterable {
    case automatic, dial, health, resources, away, skills, history, comparison
    var title: String {
        switch self {
        case .automatic: return "Current run"
        case .dial: return "Expedition dial"
        case .health: return "Health"
        case .resources: return "Resources"
        case .away: return "Since you left"
        case .skills: return "Skillbook"
        case .history: return "Recent runs"
        case .comparison: return "Compare runs"
        }
    }
}

// Presentation of the existing contract, not another game simulation or cache owner.
// The production caller supplies real time and WCSession state. Native previews can
// render the same value and views without constructing a transport or production store.
struct EOVWatchPresentation {
    let snapshot: EOVWatchSnapshot?
    let now: Double
    let reachable: Bool
    let cached: Bool
    let refreshState: EOVWatchRefreshState

    var phone: EOVWatchSnapshot.Run? { snapshot?.state == "run" ? snapshot?.run : nil }
    var history: [EOVWatchSnapshot.RecentRun] { snapshot?.recentRuns.sorted { $0.runNumber > $1.runNumber } ?? [] }
    var age: Double { max(0, now - (snapshot?.generatedAt ?? now)) }
    var ageLabel: String { eovWatchDuration(age) }
    var ending: String? { phone?.reaped == true ? "Reaped" : phone?.dead == true ? "Died" : nil }
    var heroCanAnimate: Bool {
        reachable && !cached && snapshot?.phoneInBackground == false && phone != nil && ending == nil &&
            now >= (snapshot?.generatedAt ?? now) && age <= 45
    }
    var away: EOVWatchAway? {
        guard let snapshot, let run = phone, ending == nil,
              snapshot.phoneInBackground == true, snapshot.backgroundProgressEnabled,
              let timeline = snapshot.timeline, timeline.mapId == run.mapId, now >= timeline.generatedAt else { return nil }
        var end = timeline.generatedAt + timeline.horizonSeconds
        if let forecast = snapshot.forecast {
            guard forecast.mapId == run.mapId, forecast.generatedAt == timeline.generatedAt else { return nil }
            end = min(end, forecast.forecastEnd)
        }
        let sample = timeline.sample(at: min(now, end))
        return EOVWatchAway(sample: sample, distanceReaping: timeline.distanceReaping,
            elapsed: max(0, now - timeline.generatedAt), expired: now >= end)
    }
    var refreshDescription: String {
        switch refreshState {
        case .idle: return ""
        case .requesting: return "Asking iPhone…"
        case .queued: return snapshot == nil ? "Phone refresh queued. Waiting for Echoes on iPhone." : "Phone refresh queued. The reply contains saved data; open Echoes for fresh game data."
        case .unavailable: return snapshot == nil ? "Open Echoes on your paired iPhone, then try Refresh." : "iPhone unavailable. Keeping the last snapshot and sampling any saved away estimate here."
        case .rejected: return "Update unavailable. Keeping the last valid snapshot."
        }
    }
    func source(estimated: Bool = false) -> String {
        let demo = snapshot?.demo == true ? "Demo · " : ""
        if snapshot == nil {
            return refreshState == .requesting ? "Asking iPhone…" : refreshState == .unavailable ? "iPhone unavailable" : "Open Echoes on iPhone"
        }
        if estimated, let away, away.expired || away.sample.phase == 2 {
            let stopped = away.expired ? "Estimate ended" : "Return to iPhone"
            return demo + stopped + (refreshState == .queued ? " · queued" : "")
        }
        let scope = estimated ? "Est." : "Cached"
        switch refreshState {
        case .requesting: return demo + (estimated ? "Est. · asking iPhone" : "Asking iPhone…")
        case .queued: return demo + scope + " · " + ageLabel + " · queued"
        case .unavailable: return demo + scope + " · iPhone unavailable"
        case .rejected: return demo + "Kept last snapshot"
        case .idle: break
        }
        if estimated, let away {
            if away.expired { return demo + "Estimate ended" }
            if away.sample.phase == 2 { return demo + "Return to iPhone" }
            if away.sample.phase == 1 { return demo + "Restarting · estimated" }
            return demo + "Estimated away gains"
        }
        if let ending { return demo + ending + " · " + ageLabel + " ago" }
        return demo + ((!reachable || cached) ? "Cached · " : "Phone · ") + ageLabel + " ago"
    }
}
struct EOVWatchAway {
    let sample: EOVWatchSnapshot.Timeline.Sample
    let distanceReaping: Bool
    let elapsed: Double
    let expired: Bool
    var active: Bool { !expired && sample.phase != 2 }
}

func eovWatchNumber(_ value: Double?, compact: Bool = true) -> String {
    guard let value, value.isFinite else { return "—" }
    if compact {
        for (scale, suffix) in [(1e12, "T"), (1e9, "B"), (1e6, "M"), (1e3, "K")] where value >= scale {
            return (value / scale).formatted(.number.precision(.fractionLength(value / scale >= 10 ? 0 : 1))) + suffix
        }
    }
    return value.rounded(.down).formatted(.number.precision(.fractionLength(0)))
}
func eovWatchDuration(_ value: Double) -> String {
    guard value.isFinite else { return "—" }
    let seconds = Int(max(0, min(1e12, value)))
    let tail = String(format: "%02d", seconds % 60)
    return seconds >= 3600 ? String(seconds / 3600) + ":" + String(format: "%02d", seconds / 60 % 60) + ":" + tail : String(seconds / 60) + ":" + tail
}
func eovWatchRateValue(_ amount: Double, _ seconds: Double) -> Double? {
    guard amount.isFinite, seconds.isFinite, seconds > 0 else { return nil }
    let value = amount / seconds * 60
    return value.isFinite ? value : nil
}
func eovWatchRate(_ amount: Double, _ seconds: Double) -> String { eovWatchNumber(eovWatchRateValue(amount, seconds)) }

struct EOVWatchDisplay {
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
    var reaping = false
    var phase = 0
    var expired = false
    var active = false
    var fraction: Double? { reaping && cap > 0 ? min(1, max(0, distance / cap)) : nil }
    init(presentation: EOVWatchPresentation, useAway: Bool = true) {
        guard let run = presentation.phone else { return }
        map = run.mapTitle; distance = run.distance; cap = run.cap; resources = run.resources
        tasks = Double(run.tasks); kills = Double(run.kills); health = run.healthFraction; seconds = run.durationSeconds
        reaping = run.distanceReaping != false; active = !run.dead && !run.reaped
        if useAway, let away = presentation.away {
            distance = away.sample.distance; cap = away.sample.cap; resources = away.sample.resources
            tasks = away.sample.tasks; kills = away.sample.kills; reaps = away.sample.reaps
            reaping = away.distanceReaping; phase = away.sample.phase; expired = away.expired
            estimated = true; seconds = away.elapsed; active = away.active
        }
    }
}
extension EOVWatchPresentation {
    var display: EOVWatchDisplay { EOVWatchDisplay(presentation: self) }
    func resolved(_ screen: EOVWatchScreen) -> EOVWatchScreen { screen == .automatic ? (away == nil ? .dial : .away) : screen }
}
