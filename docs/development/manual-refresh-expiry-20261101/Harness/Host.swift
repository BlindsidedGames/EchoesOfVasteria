import SwiftUI
import ActivityKit
import UIKit
import WatchConnectivity

@_silgen_name("EOVWatchCompanion_Publish")
private func publishThroughProductionObjC(_ json: UnsafePointer<CChar>)
@_silgen_name("EOVWatchCompanion_ConsumeRefreshRequest")
private func consumeThroughProductionObjC() -> Int32

// Disposable host. Production bridge, intent, attributes, phone session and widget
// are compiled directly. Only this QA cache and activities are written.
@main
struct EchoesManualExpiryHost: App {
    @Environment(\.scenePhase) private var phase
    @State private var started = false
    @State private var message = "Waiting for foreground"
    private let scenario = ProcessInfo.processInfo.arguments.first { $0.hasPrefix("--scenario=") }?
        .split(separator: "=", maxSplits: 1).last.map(String.init) ?? "limit"

    var body: some Scene {
        WindowGroup {
            VStack(spacing: 20) {
                Text("Manual expiry QA").font(.title)
                Text(scenario)
                Text(message)
                Text("Isolated native estimate. No Unity or player save.")
            }.padding().task { if phase == .active { start() } }
                .onChange(of: phase) { next in
                    if next == .active { start() }
                    receipt("phase-\(next)")
                    if next == .background && scenario == "probe" {
                        Task { @MainActor in await nativeProbe() }
                    }
                }
        }
    }

    @MainActor private func start() {
        guard !started else { return }; started = true
        do {
            let original = try Data(contentsOf: Bundle.main.url(forResource: "snapshot", withExtension: "json")!)
            var value = try JSONSerialization.jsonObject(with: original) as! [String: Any]
            // High-rate synthetic trajectory, consistently rebased to the real clock.
            // Limit crosses in 25 s; early coverage crosses in 25 s with allowance left.
            let elapsed: Double = scenario == "early" ? 35 : 3575
            let origin = Date().timeIntervalSince1970 - elapsed
            value["generatedAt"] = origin
            value["phoneInBackground"] = true
            var timeline = value["timeline"] as! [String: Any]
            timeline["generatedAt"] = origin
            value["timeline"] = timeline
            value["forecast"] = [
                "schemaVersion": 1, "sourceID": "9232cf5d055d49d4a334854dd7850f43",
                "mapId": "River", "mapTitle": "River", "sampleCount": 10,
                "generatedAt": origin, "currentDistance": 48.0,
                "projectedDistance": 212.0, "reapCap": 212.0,
                "projectedReaps": 0, "projectedTasks": 540000,
                "projectedKills": 540000, "projectedResources": 540000.0,
                "forecastEnd": origin + (scenario == "early" ? 60 : 3600), "status": "forecast"
            ] as [String: Any]
            let data = try JSONSerialization.data(withJSONObject: value)
            _ = try EOVWatchSnapshot.decode(data)
            EOVWatchSnapshotCache.save(data)
            String(decoding: data, as: UTF8.self).withCString { publishThroughProductionObjC($0) }
            try data.write(to: documents.appendingPathComponent("input.json"))
            message = "Production activity requested; deadline in 25 seconds."
            Task { @MainActor in
                for second in 1...45 {
                    try? await Task.sleep(for: .seconds(1))
                    receipt("second-\(second)")
                    if second == 2, scenario == "probe", let activity = Activity<EOVForecastAttributes>.activities.first {
                        let accepted = await EOVLiveActivityBridge.refresh(activityID: activity.id, sourceID: activity.content.state.sourceID)
                        try? JSONSerialization.data(withJSONObject: ["foregroundRefreshRejected": !accepted])
                            .write(to: documents.appendingPathComponent("foreground-guard.json"))
                    }
                }
            }
        } catch { message = String(describing: error) }
    }


    // This invokes the real WCSession delegate owner with an incoming command.
    // It proves its background/cache/reply boundary, not radio delivery.
    @MainActor private func nativeProbe() async {
        guard let activity = Activity<EOVForecastAttributes>.activities.first,
              let delegate = WCSession.default.delegate as? EOVWatchPhoneSession,
              let original = EOVWatchSnapshotCache.load() else { return }
        let oldSource = activity.content.state.sourceID
        let oldOrigin = activity.content.state.backgroundOrigin
        let wasStale = activity.activityState == .stale
        let invalidSourceRejected = !(await EOVLiveActivityBridge.refresh(activityID: activity.id, sourceID: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"))
        let reply: [String: Any] = await withCheckedContinuation { continuation in
            delegate.session(WCSession.default, didReceiveMessage: ["command": "requestSnapshot"]) { continuation.resume(returning: $0) }
        }
        let snapshot = try? EOVWatchSnapshot.decode(original)
        let receipt = (reply["estimateReceipt"] as? Data).flatMap { try? JSONDecoder().decode(EOVWatchSnapshot.EstimateReceipt.self, from: $0) }
        let acknowledged = receipt.flatMap { receipt in snapshot.map { receipt.matches($0, at: Date().timeIntervalSince1970) } } == true
        let after = Activity<EOVForecastAttributes>.activities.first { $0.id == activity.id }
        let native: [String: Any] = [
            "appBackground": UIApplication.shared.applicationState == .background,
            "unityFrameworkAbsent": !Bundle.allFrameworks.contains { $0.bundleURL.lastPathComponent == "UnityFramework.framework" },
            "wasStale": wasStale, "invalidSourceRejected": invalidSourceRejected,
            "replyEstimated": reply["refreshStatus"] as? String == "estimated", "receiptMatches": acknowledged,
            "snapshotBytesUnchanged": EOVWatchSnapshotCache.load() == original && reply["snapshot"] as? Data == original,
            "sourceAndOriginUnchanged": after?.content.state.sourceID == oldSource && after?.content.state.backgroundOrigin == oldOrigin,
            "noUnityRefreshQueued": consumeThroughProductionObjC() == 0,
            "status": after?.content.state.status ?? "missing", "resources": after?.content.state.resources ?? -1,
            "refreshRevision": after?.content.state.refreshRevision ?? 0
        ]
        if let data = try? JSONSerialization.data(withJSONObject: native, options: [.prettyPrinted, .sortedKeys]) {
            try? data.write(to: documents.appendingPathComponent("native-contracts.json"))
        }
        self.receipt("after-native-probe")
    }

    private var documents: URL { FileManager.default.urls(for: .documentDirectory, in: .userDomainMask)[0] }
    @MainActor private func receipt(_ event: String) {
        do {
            let records: [[String: Any]] = try Activity<EOVForecastAttributes>.activities.map { activity in
                let state = try JSONSerialization.jsonObject(with: JSONEncoder().encode(activity.content.state))
                let attributes = try JSONSerialization.jsonObject(with: JSONEncoder().encode(activity.attributes))
                return ["id": activity.id, "activityState": String(describing: activity.activityState),
                        "staleDate": activity.content.staleDate?.timeIntervalSince1970 ?? 0,
                        "state": state, "attributes": attributes]
            }
            let receipt: [String: Any] = ["event": event, "at": Date().timeIntervalSince1970,
                                         "appState": UIApplication.shared.applicationState.rawValue,
                                         "phoneSessionClass": NSClassFromString("EOVWatchPhoneSession") != nil,
                                         "unityFrameworkLoaded": Bundle.allFrameworks.contains { $0.bundleURL.lastPathComponent == "UnityFramework.framework" },
                                         "activities": records]
            let data = try JSONSerialization.data(withJSONObject: receipt, options: [.prettyPrinted, .sortedKeys])
            try data.write(to: documents.appendingPathComponent("\(event).json"))
        } catch { NSLog("[ManualExpiryQA] receipt failed %@", String(describing: error)) }
    }
}
