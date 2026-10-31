import SwiftUI
import WatchConnectivity

@main
struct EOVWatchCompanionApp: App {
    @StateObject private var store = EOVWatchCompanionStore()
    var body: some Scene {
        WindowGroup { CompanionHome().environmentObject(store) }
    }
}

final class EOVWatchCompanionStore: NSObject, ObservableObject, WCSessionDelegate {
    @Published private(set) var snapshot: EOVWatchSnapshot?
    @Published private(set) var reachable = false
    @Published private(set) var cached = true
    @Published private(set) var message = "Waiting for iPhone"

    override init() {
        if let data = EOVWatchSnapshotCache.load() { snapshot = try? EOVWatchSnapshot.decode(data) }
        super.init()
        message = snapshot == nil ? "Open Echoes on iPhone" : ""
        if WCSession.isSupported() {
            WCSession.default.delegate = self
            WCSession.default.activate()
        }
    }
    func refresh() {
        guard WCSession.default.activationState == .activated, WCSession.default.isReachable else {
            reachable = false
            message = snapshot == nil ? "Open Echoes on iPhone" : "iPhone unavailable · cached"
            return
        }
        let requestedAt = snapshot?.generatedAt ?? 0
        message = "Asking iPhone…"
        WCSession.default.sendMessage(["command": "requestSnapshot"], replyHandler: { reply in
            DispatchQueue.main.async {
                if let data = reply["snapshot"] as? Data { self.accept(data, cached: true) }
                if (self.snapshot?.generatedAt ?? 0) <= requestedAt {
                    self.message = self.snapshot == nil ? "Waiting for iPhone data" : "Cached reply · refresh requested"
                }
            }
        }, errorHandler: { _ in
            DispatchQueue.main.async { self.message = self.snapshot == nil ? "iPhone unavailable" : "iPhone unavailable · cached" }
        })
    }
    private func accept(_ data: Data, cached: Bool) {
        do {
            let next = try EOVWatchSnapshot.decode(data)
            if let current = snapshot,
               next.generatedAt < current.generatedAt || (cached && next.generatedAt <= current.generatedAt) { return }
            snapshot = next
            self.cached = cached
            EOVWatchSnapshotCache.save(data)
            message = ""
            NSLog("[EOVWatch] received generatedAt=%.0f cached=%d", next.generatedAt, cached)
        } catch { message = "Update unavailable · keeping cache"; NSLog("[EOVWatch] invalid update rejected") }
    }
    func session(_ session: WCSession, activationDidCompleteWith activationState: WCSessionActivationState, error: Error?) {
        DispatchQueue.main.async {
            self.reachable = session.isReachable
            if let data = session.receivedApplicationContext["snapshot"] as? Data {
                // Already-received context is a cache on relaunch, with its original timestamp.
                self.accept(data, cached: true)
            }
            NSLog("[EOVWatch] watch session activated=%d reachable=%d", activationState.rawValue, session.isReachable)
        }
    }
    func sessionReachabilityDidChange(_ session: WCSession) {
        DispatchQueue.main.async { self.reachable = session.isReachable }
    }
    func session(_ session: WCSession, didReceiveApplicationContext applicationContext: [String: Any]) {
        guard let data = applicationContext["snapshot"] as? Data else { return }
        DispatchQueue.main.async { self.accept(data, cached: applicationContext["cached"] as? Bool ?? true) }
    }
}

private struct CompanionHome: View {
    @EnvironmentObject private var store: EOVWatchCompanionStore
    var body: some View {
        NavigationStack {
            List {
                SnapshotStatus()
                NavigationLink { CurrentRunScreen() } label: { MenuLabel(title: "Current run", art: "EOVHero") }
                NavigationLink { SkillsScreen() } label: { MenuLabel(title: "Skills", art: "EOVTask") }
                NavigationLink { RecentRunsScreen() } label: { MenuLabel(title: "Recent runs", art: "EOVCarl") }
                Button("Refresh", systemImage: "arrow.clockwise") { store.refresh() }
            }.navigationTitle("Echoes")
        }
    }
}

private struct MenuLabel: View {
    let title: String
    let art: String
    var body: some View {
        HStack {
            Image(art).resizable().interpolation(.none).scaledToFit().frame(width: 22, height: 22).accessibilityHidden(true)
            Text(title)
        }
    }
}

private struct SnapshotStatus: View {
    @EnvironmentObject private var store: EOVWatchCompanionStore
    var body: some View {
        VStack(alignment: .leading, spacing: 3) {
            Text((store.snapshot?.demo == true ? "Demo · " : "") + connection)
                .font(.caption2).foregroundStyle(.secondary).lineLimit(1).minimumScaleFactor(0.8)
            if let snapshot = store.snapshot {
                Text("Updated " + Date(timeIntervalSince1970: snapshot.generatedAt).formatted(.dateTime.month(.abbreviated).day().hour(.twoDigits(amPM: .omitted)).minute(.twoDigits).second(.twoDigits)))
                    .font(.system(size: 10)).foregroundStyle(.secondary)
            }
            if !store.message.isEmpty { Text(store.message).font(.caption2).foregroundStyle(.secondary) }
        }
    }
    private var connection: String {
        guard store.snapshot != nil else { return store.reachable ? "Connected · waiting" : "iPhone unavailable" }
        return store.reachable ? (store.cached ? "Connected · cached" : "Connected") : "Offline · cached"
    }
}

private struct CurrentRunScreen: View {
    @EnvironmentObject private var store: EOVWatchCompanionStore
    var body: some View {
        GeometryReader { geometry in
            ScrollView {
                VStack(spacing: 14) {
                    CurrentRunRadial(snapshot: store.snapshot,
                        diameter: max(80, min(geometry.size.width - 16, geometry.size.height)))
                        .frame(width: geometry.size.width, height: geometry.size.height + 18)
                    VStack(spacing: 10) {
                        if let s = store.snapshot, let run = s.run, s.state == "run" {
                            Text("Phone snapshot").font(.caption2).foregroundStyle(.secondary)
                            DataRow(name: "Distance", value: metres(run.distance) + " / " + metres(run.cap))
                            DataRow(name: "Health", value: Int((run.healthFraction * 100).rounded()).formatted() + "%")
                            DataRow(name: "Time", value: duration(run.durationSeconds))
                            DataRow(name: "Tasks", value: run.tasks.formatted())
                            DataRow(name: "Kills", value: run.kills.formatted())
                            DataRow(name: "Resources", value: amount(run.resources))
                        }
                        Button("Refresh", systemImage: "arrow.clockwise") { store.refresh() }
                        SnapshotStatus()
                    }.padding(.horizontal, 10)
                }.padding(.bottom, 12)
            }
        }
        // Keep the system back button while giving the first viewport to the dial.
        .navigationTitle("").toolbarTitleDisplayMode(.inline)
    }
}

private struct CurrentRunRadial: View {
    let snapshot: EOVWatchSnapshot?
    let diameter: CGFloat
    var body: some View {
        // UI sampling only. The phone estimator already supplied every run boundary.
        TimelineView(.periodic(from: .now, by: 1)) { context in
            face(at: context.date.timeIntervalSince1970)
        }
    }
    @ViewBuilder private func face(at unix: Double) -> some View {
        if let s = snapshot, s.state == "run", let run = s.run {
            if s.phoneInBackground == true, !run.dead, !run.reaped, s.backgroundProgressEnabled,
               let timeline = s.timeline, timeline.mapId == run.mapId {
                let sample = timeline.sample(at: unix)
                DistanceResourceFace(diameter: diameter, mapTitle: run.mapTitle,
                    resources: sample.resources, estimated: true, distance: sample.distance, cap: sample.cap,
                    distanceReaping: timeline.distanceReaping, reaps: timeline.distanceReaping ? sample.reaps : nil,
                    inactive: sample.expired || sample.phase == 2,
                    status: sample.phase == 2 ? "Return to iPhone" : sample.expired ? "Estimate ended" :
                        sample.phase == 1 ? "Restarting" : timeline.truncated ? "Short forecast" : "",
                    demo: s.demo)
            } else {
                DistanceResourceFace(diameter: diameter, mapTitle: run.mapTitle,
                    resources: run.resources, estimated: false, distance: run.distance, cap: run.cap,
                    distanceReaping: run.distanceReaping, reaps: nil, inactive: run.dead || run.reaped,
                    status: run.reaped ? "Reaped" : run.dead ? "Died" : "Phone snapshot", demo: s.demo)
            }
        } else {
            DistanceResourceFace(diameter: diameter, mapTitle: "Echoes", resources: nil,
                estimated: false, distance: nil, cap: nil, distanceReaping: nil, reaps: nil, inactive: true,
                status: snapshot?.state == "town" ? "In town" : snapshot?.state == "loading" ? "Loading on iPhone" : "Open Echoes on iPhone",
                demo: snapshot?.demo ?? false)
        }
    }
}

private struct DistanceResourceFace: View {
    let diameter: CGFloat
    let mapTitle: String
    let resources: Double?
    let estimated: Bool
    let distance: Double?
    let cap: Double?
    let distanceReaping: Bool?
    let reaps: Int?
    let inactive: Bool
    let status: String
    let demo: Bool
    @Environment(\.isLuminanceReduced) private var reducedLuminance
    @ScaledMetric(relativeTo: .headline) private var headlineSize = 27
    private var accent: Color { Color(red: 0.94, green: 0.75, blue: 0.42).opacity(reducedLuminance ? 0.65 : 1) }
    private var fraction: Double? {
        guard distanceReaping == true, let distance, let cap, cap > 0 else { return nil }
        return min(1, max(0, distance / cap))
    }
    var body: some View {
        VStack(spacing: 6) {
            ZStack {
                Circle().stroke((inactive || fraction == nil ? Color.secondary : accent).opacity(0.25), lineWidth: diameter * 0.085)
                if !inactive, let fraction, fraction > 0 {
                    Circle().trim(from: 0, to: fraction)
                        .stroke(accent, style: StrokeStyle(lineWidth: diameter * 0.085, lineCap: .round))
                        .rotationEffect(.degrees(-90))
                }
                VStack(spacing: 2) {
                    Image("EOVCarl").resizable().interpolation(.none).scaledToFit()
                        .frame(width: diameter * 0.13, height: diameter * 0.13).accessibilityHidden(true)
                    Text(mapTitle).font(.system(size: min(13, diameter * 0.08), weight: .semibold))
                        .lineLimit(1).minimumScaleFactor(0.65)
                    if let resources {
                        Text(estimated ? "Est. resources" : "Actual resources")
                            .font(.system(size: 10)).foregroundStyle(.secondary)
                        Text(amount(resources)).font(.system(size: min(headlineSize, diameter * 0.19), weight: .bold, design: .rounded))
                            .lineLimit(1).minimumScaleFactor(0.6).monospacedDigit()
                    } else { Text("—").font(.title2) }
                    if let distance {
                        Text(distanceReaping != false && (cap ?? 0) > 0 ? metres(distance) + " / " + metres(cap!) : metres(distance))
                            .font(.system(size: 10)).foregroundStyle(.secondary).lineLimit(1).minimumScaleFactor(0.7)
                    }
                    if let reaps { Text("\(reaps) " + (reaps == 1 ? "reap" : "reaps")).font(.system(size: 12)).foregroundStyle(.secondary) }
                    if distanceReaping == false, resources != nil { Text("No distance reap").font(.system(size: 9)).foregroundStyle(.secondary) }
                    if demo { Text("Demo").font(.system(size: 9)).foregroundStyle(.secondary) }
                }
                .frame(width: diameter * 0.7)
                .foregroundStyle(inactive ? .secondary : .primary)
            }.frame(width: diameter, height: diameter)
            Text(status).font(.system(size: 11)).foregroundStyle(.secondary).lineLimit(1).minimumScaleFactor(0.7).frame(height: 14)
        }
    }
}

private struct SkillsScreen: View {
    @EnvironmentObject private var store: EOVWatchCompanionStore
    var body: some View {
        List {
            SnapshotStatus()
            if let s = store.snapshot {
                if s.skills.isEmpty { Text("No skills available yet") }
                ForEach(s.skills) { skill in DataRow(name: skill.name, value: "Lv " + skill.level.formatted()) }
            } else { Text("Open Echoes on iPhone to see your skills.") }
            Button("Refresh", systemImage: "arrow.clockwise") { store.refresh() }
        }.navigationTitle("Skills")
    }
}

private struct RecentRunsScreen: View {
    @EnvironmentObject private var store: EOVWatchCompanionStore
    var body: some View {
        List {
            SnapshotStatus()
            if let s = store.snapshot {
                if s.recentRuns.isEmpty { Text("No recent runs") }
                ForEach(s.recentRuns) { run in
                    NavigationLink {
                        List {
                            Text(run.mapTitle).font(.headline)
                            Text(run.estimated ? "Estimated run" : "Recorded run").font(.caption2).foregroundStyle(.secondary)
                            DataRow(name: "Result", value: run.result.capitalized)
                            DataRow(name: "Distance", value: metres(run.distance))
                            DataRow(name: "Time", value: duration(run.durationSeconds))
                            DataRow(name: "Tasks", value: run.tasks.formatted())
                            DataRow(name: "Kills", value: run.kills.formatted())
                            DataRow(name: "Resources", value: amount(run.resources))
                        }.navigationTitle("Run " + run.runNumber.formatted())
                    } label: {
                        VStack(alignment: .leading) {
                            Text(run.mapTitle)
                            Text((run.estimated ? "Est. · " : "") + metres(run.distance) + " · " + run.result.capitalized)
                                .font(.caption2).foregroundStyle(.secondary)
                        }
                    }
                }
            } else { Text("Open Echoes on iPhone to see recent adventures.") }
        }.navigationTitle("Recent runs")
    }
}

private struct DataRow: View {
    let name: String
    let value: String
    var body: some View {
        HStack(alignment: .firstTextBaseline) {
            Text(name).font(.caption2).foregroundStyle(.secondary)
            Spacer(minLength: 4)
            Text(value).multilineTextAlignment(.trailing).lineLimit(2).minimumScaleFactor(0.8)
        }
    }
}
private func amount(_ value: Double) -> String { value.formatted(.number.precision(.fractionLength(0...1))) }
private func metres(_ value: Double) -> String { amount(value) + " m" }
private func duration(_ value: Double) -> String {
    let seconds = Int(min(value, Double(Int.max / 2)).rounded(.down))
    return seconds >= 3600 ? String(format: "%d:%02d:%02d", seconds / 3600, seconds / 60 % 60, seconds % 60)
        : String(format: "%d:%02d", seconds / 60, seconds % 60)
}
