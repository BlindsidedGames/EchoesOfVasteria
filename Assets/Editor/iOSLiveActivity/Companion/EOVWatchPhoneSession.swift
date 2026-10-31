import Foundation
import WatchConnectivity
import UIKit

@objc(EOVWatchPhoneSession)
final class EOVWatchPhoneSession: NSObject, WCSessionDelegate {
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
            if let old = shared.latest.flatMap({ try? EOVWatchSnapshot.decode($0) }),
               incoming.generatedAt < old.generatedAt { return }
            shared.latest = data
            shared.cached = false
            EOVWatchSnapshotCache.save(data)
            shared.sendContext()
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

    private func sendContext() {
        guard WCSession.isSupported(), WCSession.default.activationState == .activated,
              WCSession.default.isPaired, WCSession.default.isWatchAppInstalled, let latest else { return }
        do { try WCSession.default.updateApplicationContext(["snapshot": latest, "cached": cached]) }
        catch { NSLog("[EOVWatch] context send failed: %@", String(describing: error)) }
    }
    func session(_ session: WCSession, activationDidCompleteWith activationState: WCSessionActivationState, error: Error?) {
        DispatchQueue.main.async {
            NSLog("[EOVWatch] phone session activated=%d paired=%d installed=%d reachable=%d",
                  activationState.rawValue, session.isPaired, session.isWatchAppInstalled, session.isReachable)
            self.sendContext()
        }
    }
    func sessionWatchStateDidChange(_ session: WCSession) {
        DispatchQueue.main.async { self.sendContext() }
    }
    func sessionReachabilityDidChange(_ session: WCSession) {
        DispatchQueue.main.async { self.sendContext() }
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
