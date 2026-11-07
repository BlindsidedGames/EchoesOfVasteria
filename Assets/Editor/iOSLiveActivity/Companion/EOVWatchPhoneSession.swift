import Foundation
import WatchConnectivity
import UIKit

@objc(EOVWatchPhoneSession)
final class EOVWatchPhoneSession: NSObject, WCSessionDelegate {
    private static let contextInterval: TimeInterval = 15
    private static var instance: EOVWatchPhoneSession?
    private static var shared: EOVWatchPhoneSession {
        if let instance { return instance }
        let service = EOVWatchPhoneSession()
        instance = service
        return service
    }
    private var latest = EOVWatchSnapshotCache.load()
    private var refreshRequested = false
    private var cached = true
    private var nextContextUpdate: TimeInterval = 0
    private var liveMessageStartedAt: TimeInterval?
    private var liveUpdatesStalled = false

    private override init() {
        super.init()
        if WCSession.isSupported() {
            WCSession.default.delegate = self
            WCSession.default.activate()
        }
    }

    @objc(publish:)
    static func publish(_ json: NSString) {
        let data = Data((json as String).utf8)
        do {
            let incoming = try EOVWatchSnapshot.decode(data)
            let old = shared.latest.flatMap { try? EOVWatchSnapshot.decode($0) }
            if let old, incoming.generatedAt < old.generatedAt { return }
            shared.latest = data
            shared.cached = false
            let changedMode = old?.state != incoming.state || old?.phoneInBackground != incoming.phoneInBackground
            if changedMode || !liveUpdatesAvailable() || ProcessInfo.processInfo.systemUptime >= shared.nextContextUpdate {
                shared.sendContext()
            }
            shared.sendLiveSnapshot()
            NSLog("[EOVWatch] published generatedAt=%.0f state=%@ skills=%d runs=%d",
                  incoming.generatedAt, incoming.state, incoming.skills.count, incoming.recentRuns.count)
        } catch { NSLog("[EOVWatch] rejected snapshot: %@", String(describing: error)) }
    }

    @objc(consumeRefreshRequest)
    static func consumeRefreshRequest() -> Int32 {
        guard let service = instance else { return 0 }
        let value = service.refreshRequested
        service.refreshRequested = false
        return value ? 1 : 0
    }

    @objc(liveUpdatesAvailable)
    static func liveUpdatesAvailable() -> Bool {
        guard canSendLiveMessage else { return false }
        guard let service = instance else { return true }
        guard !service.liveUpdatesStalled else { return false }
        guard let started = service.liveMessageStartedAt else { return true }
        return ProcessInfo.processInfo.systemUptime - started < contextInterval
    }

    private static var canSendLiveMessage: Bool {
        WCSession.isSupported() && WCSession.default.activationState == .activated &&
            WCSession.default.isReachable && UIApplication.shared.applicationState == .active
    }

    private func sendContext() {
        guard let latest else { return }
        EOVWatchSnapshotCache.save(latest)
        nextContextUpdate = ProcessInfo.processInfo.systemUptime + Self.contextInterval
        guard WCSession.isSupported(), WCSession.default.activationState == .activated,
              WCSession.default.isPaired, WCSession.default.isWatchAppInstalled else { return }
        do { try WCSession.default.updateApplicationContext(["snapshot": latest, "cached": cached]) }
        catch { NSLog("[EOVWatch] context send failed: %@", String(describing: error)) }
    }

    private func sendLiveSnapshot() {
        guard Self.canSendLiveMessage, !cached, let data = latest else { return }
        let started = ProcessInfo.processInfo.systemUptime
        if let previous = liveMessageStartedAt {
            guard started - previous >= Self.contextInterval else { return }
            // Reachability can remain true after disconnect. Retry only with the slow feed
            // until a reply confirms delivery, and ignore callbacks from expired attempts.
            liveUpdatesStalled = true
        }
        liveMessageStartedAt = started
        WCSession.default.sendMessage(["snapshot": data, "cached": false], replyHandler: { _ in
            DispatchQueue.main.async {
                guard self.liveMessageStartedAt == started else { return }
                self.liveMessageStartedAt = nil
                self.liveUpdatesStalled = false
                if self.latest != data { self.sendLiveSnapshot() }
            }
        }, errorHandler: { error in
            DispatchQueue.main.async {
                guard self.liveMessageStartedAt == started else { return }
                self.liveMessageStartedAt = nil
                self.liveUpdatesStalled = true
                self.sendContext()
                NSLog("[EOVWatch] live send failed: %@", String(describing: error))
            }
        })
    }
    func session(_ session: WCSession, activationDidCompleteWith activationState: WCSessionActivationState, error: Error?) {
        DispatchQueue.main.async {
            NSLog("[EOVWatch] phone session activated=%d paired=%d installed=%d reachable=%d",
                  activationState.rawValue, session.isPaired, session.isWatchAppInstalled, session.isReachable)
            self.sendContext()
            self.sendLiveSnapshot()
        }
    }
    func sessionWatchStateDidChange(_ session: WCSession) {
        DispatchQueue.main.async { self.sendContext(); self.sendLiveSnapshot() }
    }
    func sessionReachabilityDidChange(_ session: WCSession) {
        DispatchQueue.main.async { self.sendContext(); self.sendLiveSnapshot() }
    }
    func sessionDidBecomeInactive(_ session: WCSession) {}
    func sessionDidDeactivate(_ session: WCSession) { session.activate() }
    func session(_ session: WCSession, didReceiveMessage message: [String: Any], replyHandler: @escaping ([String: Any]) -> Void) {
        DispatchQueue.main.async {
            guard message["command"] as? String == "requestSnapshot" else {
                replyHandler(["error": "unsupportedRequest"]); return
            }
            if UIApplication.shared.applicationState != .background {
                self.refreshRequested = true
                var reply: [String: Any] = ["cached": true, "refreshStatus": "queued"]
                if let data = self.latest { reply["snapshot"] = data }
                replyHandler(reply)
                return
            }
            // Unity remains paused. Evaluate the native cache and, if present,
            // update the phone's existing activity through its normal owner.
            Task { @MainActor in
                guard let data = self.latest, let snapshot = try? EOVWatchSnapshot.decode(data),
                      let estimate = snapshot.backgroundEstimate(at: Date().timeIntervalSince1970) else {
                    replyHandler(self.cachedReply()); return
                }
                if #available(iOS 16.2, *), let source = estimate.sourceID {
                    await EOVLiveActivityBridge.refreshCurrentEstimate(sourceID: source)
                }
                // Publication or foregrounding during await invalidates this reply.
                guard self.latest == data, UIApplication.shared.applicationState == .background else {
                    replyHandler(self.cachedReply()); return
                }
                let now = Date().timeIntervalSince1970
                let receipt = EOVWatchSnapshot.EstimateReceipt(mapId: snapshot.run!.mapId,
                    sourceID: estimate.sourceID, origin: estimate.origin, end: estimate.end, evaluatedAt: now)
                guard receipt.matches(snapshot, at: now), let encoded = try? JSONEncoder().encode(receipt) else {
                    replyHandler(self.cachedReply()); return
                }
                replyHandler(["snapshot": data, "cached": true, "refreshStatus": "estimated", "estimateReceipt": encoded])
                NSLog("[EOVWatch] native estimate refreshed; gameplay snapshot unchanged")
            }
        }
    }
    private func cachedReply() -> [String: Any] {
        var reply: [String: Any] = ["cached": true, "refreshStatus": "unavailable"]
        if let data = latest { reply["snapshot"] = data }
        return reply
    }
}
