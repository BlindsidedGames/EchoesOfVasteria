import ActivityKit
import Foundation
import UIKit

// This class never estimates progress, writes saves, or schedules background work.
@available(iOS 16.2, *)
@MainActor
@objc(EOVLiveActivityBridge)
public final class EOVLiveActivityBridge: NSObject {
    private static var generation = 0
    private static var pending: Task<Void, Never>?

    @objc(isSupported)
    public static func isSupported() -> Bool {
        (Bundle.main.object(forInfoDictionaryKey: "NSSupportsLiveActivities") as? Bool) == true
            && ActivityAuthorizationInfo().areActivitiesEnabled
    }

    @objc(startOrUpdate:)
    public static func startOrUpdate(_ json: NSString) {
        do {
            let data = Data((json as String).utf8)
            // Reserve room for immutable attributes within ActivityKit's combined 4 KB limit.
            guard data.count <= 3500 else { throw EOVForecastAttributes.ForecastError.payloadTooLarge }
            let state = try JSONDecoder().decode(EOVForecastAttributes.ContentState.self, from: data)
            try state.validate()
            guard state.status == "forecast", state.sampleCount > 0,
                  state.forecastEnd > Date().timeIntervalSince1970 else { end(); return }
            guard isSupported() else { NSLog("[EOVLiveActivity] unavailable or disabled"); return }
            generation += 1
            let command = generation
            let previous = pending
            pending = Task { @MainActor in
                await previous?.value
                defer { if command == generation { pending = nil } }
                guard command == generation, !Task.isCancelled else { return }
                let content = ActivityContent(state: state,
                    staleDate: Date(timeIntervalSince1970: state.forecastEnd))
                if let activity = Activity<EOVForecastAttributes>.activities.first(where: { $0.attributes.mapId == state.mapId }) {
                    await activity.update(content)
                    guard command == generation, !Task.isCancelled else { return }
                    NSLog("[EOVLiveActivity] updated id=%@", activity.id)
                } else {
                    // An existing activity may update on pause. A new one starts only in foreground.
                    guard UIApplication.shared.applicationState == .active else {
                        NSLog("[EOVLiveActivity] skipped start while not foreground"); return
                    }
                    for old in Activity<EOVForecastAttributes>.activities {
                        await old.end(nil, dismissalPolicy: .immediate)
                    }
                    guard command == generation, !Task.isCancelled else { return }
                    do {
                        let activity = try Activity.request(
                            attributes: EOVForecastAttributes(mapId: state.mapId),
                            content: content, pushType: nil)
                        NSLog("[EOVLiveActivity] started id=%@", activity.id)
                    } catch {
                        NSLog("[EOVLiveActivity] start failed: %@", String(describing: error))
                    }
                }
            }
        } catch {
            NSLog("[EOVLiveActivity] rejected forecast: %@", String(describing: error))
        }
    }

    @objc(end)
    public static func end() {
        generation += 1
        let command = generation
        let previous = pending
        pending = Task { @MainActor in
            await previous?.value
            defer { if command == generation { pending = nil } }
            for activity in Activity<EOVForecastAttributes>.activities {
                await activity.end(nil, dismissalPolicy: .immediate)
                NSLog("[EOVLiveActivity] ended id=%@", activity.id)
            }
        }
    }
}
