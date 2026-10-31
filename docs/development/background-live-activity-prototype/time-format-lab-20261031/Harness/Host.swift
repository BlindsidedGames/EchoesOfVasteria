import SwiftUI
import ActivityKit
import OSLog

private let logger = Logger(subsystem: "com.blindsidedgames.timeformatlab", category: "host")

@main struct TimeFormatHost: App {
    @Environment(\.scenePhase) var phase
    @State private var started = false
    @State private var message = "Waiting for foreground"
    var body: some Scene {
        WindowGroup {
            VStack(spacing: 20) {
                Text("Time Format Lab").font(.largeTitle)
                Text(message)
                Text("Disposable ActivityKit experiment. One request per activity, no updates, no timer, no networking.")
            }.padding().onChange(of: phase) { _, next in
                event("phase", fields: ["phase": String(describing: next)])
                if next == .active { startOnce() }
            }.task { if phase == .active { startOnce() } }
        }
    }
    func startOnce() {
        guard !started else { return }; started = true
        let args = ProcessInfo.processInfo.arguments
        let variant = args.firstIndex(of: "--trial").flatMap { $0 + 1 < args.count ? args[$0+1] : nil } ?? "attributed"
        Task { @MainActor in
            for a in Activity<ClockLabAttributes>.activities { await a.end(nil, dismissalPolicy: .immediate) }
            let origin = Date().addingTimeInterval(30)
            let run = UUID().uuidString
            do {
                var ids: [String] = []
                for kind in [variant] {
                    let a = try Activity.request(attributes: ClockLabAttributes(variant: kind, run: run), content: ActivityContent(state: .init(origin: origin), staleDate: origin.addingTimeInterval(180)), pushType: nil)
                    ids.append(a.id)
                }
                let receipt: [String: Any] = ["run": run, "variant": variant, "origin": origin.timeIntervalSince1970, "requestCompletedAt": Date().timeIntervalSince1970, "activityIDs": ids, "pid": ProcessInfo.processInfo.processIdentifier, "updates": 0, "networking": false, "startDistance": 2000, "cap": 38300, "pace": 10]
                let data = try JSONSerialization.data(withJSONObject: receipt, options: [.prettyPrinted, .sortedKeys])
                try data.write(to: documents.appendingPathComponent("start.json"), options: .atomic)
                event("requested", fields: receipt)
                message = "Requested \(variant) alone. Origin \(origin.timeIntervalSince1970)"
            } catch { event("requestError", fields: ["error": String(describing: error)]); message = String(describing: error) }
        }
    }
    var documents: URL { FileManager.default.urls(for: .documentDirectory, in: .userDomainMask)[0] }
    func event(_ name: String, fields: [String: Any]) {
        var record = fields; record["event"] = name; record["at"] = Date().timeIntervalSince1970
        let data = try! JSONSerialization.data(withJSONObject: record, options: [.sortedKeys]) + Data([10])
        let url = documents.appendingPathComponent("events.jsonl")
        if !FileManager.default.fileExists(atPath: url.path) { FileManager.default.createFile(atPath: url.path, contents: nil) }
        if let handle = try? FileHandle(forWritingTo: url) { try? handle.seekToEnd(); try? handle.write(contentsOf: data); try? handle.close() }
        logger.notice("\(name, privacy: .public) \(String(data: data, encoding: .utf8)!, privacy: .public)")
    }
}
