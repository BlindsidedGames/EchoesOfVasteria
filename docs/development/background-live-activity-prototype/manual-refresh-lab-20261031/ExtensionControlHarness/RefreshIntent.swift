import AppIntents
import ActivityKit
import Foundation

struct RefreshSnapshotIntent: AppIntent {
    static var title: LocalizedStringResource = "Refresh synthetic snapshot"
    static var supportedModes: IntentModes { .background }
    static var authenticationPolicy: IntentAuthenticationPolicy { .alwaysAllowed }
    static var isDiscoverable: Bool { false }
    @Parameter(title: "Run") var run: String
    init() {}
    init(run: String) { self.run = run }
    func perform() async throws -> some IntentResult {
        let now = Date()
        let activities = Activity<RefreshLabAttributes>.activities
        labEvent("intent.start",["run":run,"activities":activities.map(\.id),"modeRequested":"background","intentProtocol":"AppIntent","targetPlacement":"widget only"])
        for activity in activities where activity.attributes.run == run {
            let count = activity.content.state.refreshCount+1
            let s = RefreshLabAttributes.sample(origin: activity.attributes.origin, now: now, count: count, executor: Bundle.main.bundleIdentifier ?? "unknown")
            labEvent("intent.sample",["activityID":activity.id,"origin":activity.attributes.origin.timeIntervalSince1970,"sampledAt":now.timeIntervalSince1970,"distance":s.distance,"cap":200,"resources":s.resources,"reaps":s.reaps,"segment":s.segment,"refreshCount":count])
            await activity.update(ActivityContent(state:s,staleDate:nil))
            labEvent("intent.updated",["activityID":activity.id,"refreshCount":count])
        }
        labEvent("intent.end")
        return .result()
    }
}
