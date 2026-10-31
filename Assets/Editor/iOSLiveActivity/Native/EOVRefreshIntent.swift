import AppIntents
import Foundation

@available(iOS 17.0, *)
struct EOVRefreshIntent: LiveActivityIntent {
    static var title: LocalizedStringResource = "Refresh outing estimate"
    static var openAppWhenRun: Bool { false }
    @available(iOS 26.0, *)
    static var supportedModes: IntentModes { .background }
    static var authenticationPolicy: IntentAuthenticationPolicy { .alwaysAllowed }
    static var isDiscoverable: Bool { false }
    @Parameter(title: "Activity") var activityID: String
    @Parameter(title: "Source") var sourceID: String
    init() {}
    init(activityID: String, sourceID: String) { self.activityID = activityID; self.sourceID = sourceID }
    func perform() async throws -> some IntentResult {
        #if EOV_LIVE_ACTIVITY_HOST
        await EOVLiveActivityBridge.refresh(activityID: activityID, sourceID: sourceID)
        #endif
        return .result()
    }
}
