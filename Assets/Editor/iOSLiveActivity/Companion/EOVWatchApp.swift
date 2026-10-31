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
        List {
            SnapshotStatus()
            if let s = store.snapshot {
                if let f = s.forecast, s.backgroundProgressEnabled, f.status == "forecast" {
                    Section("Estimate") {
                        CompanionForecast(forecast: f)
                    }
                }
                if s.state == "loading" { Text("iPhone is loading") }
                else if let run = s.run, s.state == "run" {
                    Section(run.mapTitle) {
                        Text(run.dead ? (run.reaped ? "Reaped" : "Died") : "Adventure in progress")
                        DataRow(name: "Distance", value: metres(run.distance) + " / " + metres(run.cap))
                        DataRow(name: "Health", value: Int((run.healthFraction * 100).rounded()).formatted() + "%")
                        DataRow(name: "Time", value: duration(run.durationSeconds))
                        DataRow(name: "Tasks", value: run.tasks.formatted())
                        DataRow(name: "Kills", value: run.kills.formatted())
                        DataRow(name: "Resources", value: amount(run.resources))
                    }
                } else { Text(s.state == "town" ? "In town" : "No active run") }
            } else { Text("Open Echoes on iPhone to see your current run.") }
            Button("Refresh", systemImage: "arrow.clockwise") { store.refresh() }
        }.navigationTitle("Current run")
    }
}

private struct CompanionForecast: View {
    let forecast: EOVWatchSnapshot.Forecast
    @Environment(\.scenePhase) private var scenePhase
    @State private var expired = false
    var body: some View {
        EOVWatchForecastView(mapTitle: forecast.mapTitle, projectedDistance: forecast.projectedDistance,
            projectedReaps: forecast.projectedReaps, timeRange: forecast.timeRange,
            stale: expired || forecast.forecastEnd <= Date().timeIntervalSince1970, ended: false)
            .task(id: forecast.forecastEnd) {
                let remaining = forecast.forecastEnd - Date().timeIntervalSince1970
                expired = remaining <= 0
                guard remaining > 0 else { return }
                do { try await Task.sleep(for: .seconds(remaining)) } catch { return }
                guard !Task.isCancelled else { return }
                expired = true
            }
            .onChange(of: scenePhase) { _, phase in
                if phase == .active { expired = forecast.forecastEnd <= Date().timeIntervalSince1970 }
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
