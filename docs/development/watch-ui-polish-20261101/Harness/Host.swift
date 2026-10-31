import SwiftUI
import ActivityKit

// Isolated host only: exact production widget, bridge, intent and decoder compile
// unchanged. This host creates a QA activity from a controlled captured trajectory.
@main
struct EchoesUIPolishHost: App {
    @Environment(\.scenePhase) private var phase
    @State private var started = false
    @State private var message = "Waiting for foreground"
    var body: some Scene {
        WindowGroup {
            VStack(spacing: 20) {
                Text("Echoes UI Polish").font(.title)
                Text(message)
                Text("Isolated native UI validation. No Unity, player save or production cache.")
            }.padding().task { if phase == .active { start() } }
                .onChange(of: phase) { next in if next == .active { start() } }
        }
    }
    @MainActor private func start() {
        guard !started else { return }; started = true
        do {
            let original = try Data(contentsOf: Bundle.main.url(forResource: "snapshot", withExtension: "json")!)
            var value = try JSONSerialization.jsonObject(with: original) as! [String: Any]
            let origin = Date().timeIntervalSince1970 - 30
            let shift = origin - (value["generatedAt"] as! Double)
            value["generatedAt"] = origin
            value["phoneInBackground"] = true
            for key in ["timeline", "forecast"] {
                if var part = value[key] as? [String: Any] {
                    part["generatedAt"] = (part["generatedAt"] as! Double) + shift
                    if let end = part["forecastEnd"] as? Double { part["forecastEnd"] = end + shift }
                    value[key] = part
                }
            }
            let data = try JSONSerialization.data(withJSONObject: value)
            _ = try EOVWatchSnapshot.decode(data)
            EOVWatchSnapshotCache.save(data)
            EOVLiveActivityBridge.publishSnapshot(String(decoding: data, as: UTF8.self) as NSString)
            message = "Production widget requested. Lock Screen or expand Dynamic Island."
            Task { @MainActor in
                try? await Task.sleep(for: .seconds(2))
                let records = Activity<EOVForecastAttributes>.activities.map { activity in
                    ["id": activity.id, "state": String(describing: activity.activityState), "origin": String(activity.content.state.backgroundOrigin ?? 0)]
                }
                let receipt = try JSONSerialization.data(withJSONObject: records, options: .prettyPrinted)
                try receipt.write(to: FileManager.default.urls(for: .documentDirectory, in: .userDomainMask)[0].appendingPathComponent("activity.json"))
            }
        } catch { message = String(describing: error) }
    }
}
