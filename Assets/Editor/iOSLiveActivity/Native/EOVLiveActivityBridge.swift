import ActivityKit
import Foundation
import UIKit

// Presentation only. This owner reads the managed trajectory; it never enters Unity,
// checkpoints, settles rewards, changes gameplay time, or requests background time.
@available(iOS 16.2, *)
@MainActor
@objc(EOVLiveActivityBridge)
public final class EOVLiveActivityBridge: NSObject {
    private static var generation = 0
    private static var pending: Task<Void, Never>?
    private static let sourceKey = "EOVLiveActivity.ValidSource"

    @objc(isSupported)
    public static func isSupported() -> Bool {
        (Bundle.main.object(forInfoDictionaryKey: "NSSupportsLiveActivities") as? Bool) == true
            && ActivityAuthorizationInfo().areActivitiesEnabled
    }

    // Called only after the Watch transport accepted and atomically cached this data.
    @objc(publishSnapshot:)
    public static func publishSnapshot(_ json: NSString) {
        let data = Data((json as String).utf8)
        guard EOVWatchSnapshotCache.load() == data else { return }
        guard let snapshot = try? EOVWatchSnapshot.decode(data),
              let state = content(snapshot, at: Date().timeIntervalSince1970),
              let run = snapshot.run, isSupported() else { end(); return }
        UserDefaults.standard.set(state.sourceID, forKey: sourceKey)
        generation += 1
        let command = generation, previous = pending
        pending = Task { @MainActor in
            await previous?.value
            defer { if command == generation { pending = nil } }
            guard command == generation else { return }
            if let activity = Activity<EOVForecastAttributes>.activities.first(where: { $0.attributes.mapId == run.mapId && $0.activityState == .active }) {
                guard let next = presentation(state, after: activity.content.state, attributes: activity.attributes) else { return }
                await activity.update(ActivityContent(state: next, staleDate: Date(timeIntervalSince1970: next.forecastEnd)))
                NSLog("[EOVLiveActivity] published id=%@ status=%@ distance=%.2f cap=%.2f", activity.id, state.status, state.distance, state.cap)
            } else {
                // A cached background snapshot can update an existing activity, never start one.
                guard UIApplication.shared.applicationState == .active else { return }
                for old in Activity<EOVForecastAttributes>.activities { await old.end(nil, dismissalPolicy: .immediate) }
                guard command == generation else { return }
                do {
                    let attributes = EOVForecastAttributes(mapId: run.mapId)
                    guard let next = presentation(state, attributes: attributes) else { return }
                    let content = ActivityContent(state: next, staleDate: Date(timeIntervalSince1970: next.forecastEnd))
                    let activity = try Activity.request(attributes: attributes, content: content, pushType: nil)
                    NSLog("[EOVLiveActivity] started id=%@ source=%@ portraitLayers=%d", activity.id, state.sourceID, next.portraitLayers?.count ?? 0)
                } catch { NSLog("[EOVLiveActivity] start failed: %@", String(describing: error)) }
            }
        }
    }

    // All three commands share a queue. New publication/End invalidates queued old taps;
    // no action requests an activity, and a duplicate tap resamples the same origin.
    public static func refresh(activityID: String, sourceID: String) async {
        guard UserDefaults.standard.string(forKey: sourceKey) == sourceID else { return }
        generation += 1
        let command = generation, previous = pending
        let task = Task { @MainActor in
            await previous?.value
            defer { if command == generation { pending = nil } }
            let now = Date().timeIntervalSince1970
            guard command == generation,
                  isSupported(),
                  UIApplication.shared.applicationState == .background,
                  UserDefaults.standard.string(forKey: sourceKey) == sourceID,
                  let data = EOVWatchSnapshotCache.load(), let snapshot = try? EOVWatchSnapshot.decode(data),
                  snapshot.phoneInBackground == true,
                  let state = content(snapshot, at: now), state.sourceID == sourceID,
                  let activity = Activity<EOVForecastAttributes>.activities.first(where: { $0.id == activityID }),
                  activity.activityState == .active, activity.attributes.mapId == snapshot.run?.mapId,
                  activity.content.state.sourceID == sourceID,
                  now >= activity.content.state.sampledAt else {
                NSLog("[EOVLiveActivity] ignored stale/inactive refresh id=%@", activityID); return
            }
            let before = UIApplication.shared.applicationState.rawValue
            guard let next = presentation(state, after: activity.content.state, attributes: activity.attributes, manualRefresh: true) else { return }
            await activity.update(ActivityContent(state: next, staleDate: Date(timeIntervalSince1970: next.forecastEnd)))
            NSLog("[EOVLiveActivity] refresh id=%@ source=%@ pid=%d bundle=%@ appStateBefore=%d appStateAfter=%d sampledAt=%.6f distance=%.4f cap=%.4f resources=%.4f reaps=%d tasks=%@ kills=%@ origin=%.0f status=%@", activity.id, sourceID, ProcessInfo.processInfo.processIdentifier, Bundle.main.bundleIdentifier ?? "", before, UIApplication.shared.applicationState.rawValue, state.sampledAt, state.distance, state.cap, state.resources, state.reaps, state.tasks.map { String($0) } ?? "unknown", state.kills.map { String($0) } ?? "unknown", state.backgroundOrigin ?? 0, state.status)
            NSLog("[EOVLiveActivity] presentation portraitLayers=%d refreshRevision=%llu reapRevision=%llu", next.portraitLayers?.count ?? 0, UInt64(next.refreshRevision ?? 0), UInt64(next.reapRevision ?? 0))
        }
        pending = task
        await task.value
    }

    // Shared by awake publication and background intent. The same validated sampler
    // already used on Watch owns effective caps, restart boundaries and resource deltas.
    private static func content(_ snapshot: EOVWatchSnapshot, at now: Double) -> EOVForecastAttributes.ContentState? {
        guard snapshot.backgroundProgressEnabled, snapshot.state == "run",
              let run = snapshot.run, let forecast = snapshot.forecast,
              let source = forecast.sourceID, source.count == 32,
              forecast.status == "forecast", forecast.sampleCount > 0,
              let timeline = snapshot.timeline, timeline.mapId == run.mapId, forecast.mapId == run.mapId,
              forecast.generatedAt == timeline.generatedAt,
              now >= timeline.generatedAt, now < min(forecast.forecastEnd, timeline.generatedAt + timeline.horizonSeconds)
        else { return nil }
        let background = snapshot.phoneInBackground == true
        let sample = timeline.sample(at: now)
        return .init(sourceID: source, mapTitle: run.mapTitle,
            sampledAt: background ? now : snapshot.generatedAt,
            backgroundOrigin: background ? timeline.generatedAt : nil,
            forecastEnd: min(forecast.forecastEnd, timeline.generatedAt + timeline.horizonSeconds),
            distance: background ? sample.distance : run.distance,
            cap: background ? sample.cap : run.cap,
            resources: background ? sample.resources : run.resources,
            tasks: background ? sample.tasks : Double(run.tasks),
            kills: background ? sample.kills : Double(run.kills),
            reaps: background ? sample.reaps : 0,
            status: !background ? "live" : sample.phase == 2 ? "waiting" : sample.phase == 1 ? "restarting" : "estimate",
            portraitLayers: snapshot.portraitLayers)
    }

    private static func presentation(_ state: EOVForecastAttributes.ContentState,
                                     after previous: EOVForecastAttributes.ContentState? = nil,
                                     attributes: EOVForecastAttributes,
                                     manualRefresh: Bool = false) -> EOVForecastAttributes.ContentState? {
        var next = state
        next.refreshRevision = (previous?.refreshRevision ?? 0) &+ (manualRefresh ? 1 : 0)
        let gainedReap = previous.map {
            state.backgroundOrigin != nil && state.backgroundOrigin == $0.backgroundOrigin &&
                state.sourceID == $0.sourceID && state.reaps > $0.reaps
        } ?? false
        next.reapRevision = (previous?.reapRevision ?? 0) &+ (gainedReap ? 1 : 0)
        // ActivityKit bounds attributes and state together. Portraits are optional;
        // shedding one must not suppress otherwise valid forecast presentation.
        guard let attributesData = try? JSONEncoder().encode(attributes) else { return nil }
        func bytes(_ value: EOVForecastAttributes.ContentState) -> Int {
            ((try? JSONEncoder().encode(value).count) ?? 4096) + attributesData.count
        }
        if bytes(next) > 3900 { next.portraitLayers = nil }
        let payloadBytes = bytes(next)
        guard payloadBytes <= 3900 else { return nil }
        if manualRefresh || previous == nil {
            NSLog("[EOVLiveActivity] payload bytes=%d portraitLayers=%d reduceMotion=%d", payloadBytes, next.portraitLayers?.count ?? 0, UIAccessibility.isReduceMotionEnabled ? 1 : 0)
        }
        return next
    }

    @objc(end)
    public static func end() {
        // Persist invalidation before any suspension or async end: old cached snapshots
        // and buttons cannot re-enable an opted-out/retired session after a cold launch.
        UserDefaults.standard.removeObject(forKey: sourceKey)
        generation += 1
        let command = generation, previous = pending
        pending = Task { @MainActor in
            await previous?.value
            defer { if command == generation { pending = nil } }
            guard command == generation else { return }
            for activity in Activity<EOVForecastAttributes>.activities {
                await activity.end(nil, dismissalPolicy: .immediate)
                NSLog("[EOVLiveActivity] ended id=%@", activity.id)
            }
        }
    }
}
