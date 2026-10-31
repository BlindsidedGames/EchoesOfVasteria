import Foundation
import WatchConnectivity

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
            self.refreshRequested = true
            // Only Unity can generate a new snapshot. This acknowledgement is cache only.
            if let data = self.latest { replyHandler(["snapshot": data, "cached": true]) }
            else { replyHandler(["cached": true, "waitingForPhone": true]) }
            NSLog("[EOVWatch] refresh queued; reply cached=%d", self.latest != nil)
        }
    }
}
