import SwiftUI
import ActivityKit

@main struct TimeFormatHost: App {
    @Environment(\.scenePhase) var phase
    @State private var started = false
    @State private var message = "Disposable Refresh lab"
    init() { labEvent("host.init") }
    var body: some Scene {
        WindowGroup {
            VStack { Text("Refresh Lab").font(.largeTitle); Text(message); Text("Synthetic snapshot only. No server, Unity, rewards or background timer.") }.padding()
                .onChange(of: phase) { _, next in
                    labEvent("host.phase", ["phase": String(describing: next)])
                    if next == .active { startOnce() }
                }.task { if phase == .active { startOnce() } }
        }
    }
    func startOnce() {
        guard !started else { return }; started = true
        Task { @MainActor in
            for a in Activity<RefreshLabAttributes>.activities { await a.end(nil, dismissalPolicy: .immediate) }
            let origin = Date().addingTimeInterval(50)
            let attributes = RefreshLabAttributes(origin: origin, run: UUID().uuidString)
            do {
                let state = RefreshLabAttributes.sample(origin: origin, now: origin, count: 0, executor: "initial host")
                let a = try Activity.request(attributes: attributes, content: ActivityContent(state: state, staleDate: nil), pushType: nil)
                let rec: [String:Any] = ["origin":origin.timeIntervalSince1970,"activityID":a.id,"run":attributes.run,"pid":ProcessInfo.processInfo.processIdentifier,"at":Date().timeIntervalSince1970]
                let data = try JSONSerialization.data(withJSONObject: rec, options: [.sortedKeys,.prettyPrinted])
                try data.write(to: FileManager.default.urls(for: .documentDirectory, in: .userDomainMask)[0].appendingPathComponent("refresh-request.json"))
                labEvent("host.requested", rec);message = "Activity requested. Lock screen; tap Refresh after origin."
            } catch {labEvent("host.error",["error":String(describing:error)])}
        }
    }
}
