import Foundation
import Combine
import WatchConnectivity

final class EOVWatchCompanionStore: NSObject, ObservableObject, WCSessionDelegate {
    @Published private(set) var snapshot: EOVWatchSnapshot?
    @Published private(set) var reachable = false
    @Published private(set) var cached = true
    @Published private(set) var refreshState = EOVWatchRefreshState.idle
    private var refreshGeneration = 0

    override init() {
        if let data = EOVWatchSnapshotCache.load() { snapshot = try? EOVWatchSnapshot.decode(data) }
        super.init()
        if WCSession.isSupported() {
            WCSession.default.delegate = self
            WCSession.default.activate()
        }
    }
    func refresh() {
        guard refreshState != .requesting else { return }
        guard WCSession.isSupported(), WCSession.default.activationState == .activated,
              WCSession.default.isReachable else {
            reachable = false
            refreshState = .unavailable
            return
        }
        refreshGeneration += 1
        let generation = refreshGeneration
        let requestedAt = snapshot?.generatedAt ?? 0
        refreshState = .requesting
        WCSession.default.sendMessage(["command": "requestSnapshot"], replyHandler: { reply in
            DispatchQueue.main.async {
                guard generation == self.refreshGeneration else { return }
                if let data = reply["snapshot"] as? Data, !self.accept(data, cached: true) {
                    // A stale reply cannot replace a newer context, or hide a rejection.
                    if self.refreshState == .rejected { return }
                }
                self.refreshState = (self.snapshot?.generatedAt ?? 0) > requestedAt && !self.cached ? .idle : .queued
            }
        }, errorHandler: { _ in
            DispatchQueue.main.async {
                guard generation == self.refreshGeneration else { return }
                self.reachable = WCSession.default.isReachable
                self.refreshState = .unavailable
            }
        })
    }
    @discardableResult private func accept(_ data: Data, cached: Bool) -> Bool {
        do {
            let next = try EOVWatchSnapshot.decode(data)
            if let current = snapshot,
               next.generatedAt < current.generatedAt || (cached && next.generatedAt <= current.generatedAt) { return false }
            snapshot = next
            self.cached = cached
            EOVWatchSnapshotCache.save(data)
            refreshState = .idle
            NSLog("[EOVWatch] received generatedAt=%.0f cached=%d", next.generatedAt, cached)
            return true
        } catch {
            refreshState = .rejected
            NSLog("[EOVWatch] invalid update rejected; retained prior snapshot")
            return false
        }
    }
    private func receiveContext(_ context: [String: Any]) {
        guard let data = context["snapshot"] as? Data else { return }
        if accept(data, cached: context["cached"] as? Bool ?? true) {
            // Once a new context arrives, late callbacks from an old request have no UI authority.
            refreshGeneration += 1
        }
    }
    func session(_ session: WCSession, activationDidCompleteWith activationState: WCSessionActivationState, error: Error?) {
        DispatchQueue.main.async {
            self.reachable = session.isReachable
            if let data = session.receivedApplicationContext["snapshot"] as? Data { self.accept(data, cached: true) }
            NSLog("[EOVWatch] activated=%d reachable=%d", activationState.rawValue, session.isReachable)
        }
    }
    func sessionReachabilityDidChange(_ session: WCSession) {
        DispatchQueue.main.async {
            self.reachable = session.isReachable
            if self.reachable && self.refreshState == .unavailable { self.refreshState = .idle }
        }
    }
    func session(_ session: WCSession, didReceiveApplicationContext applicationContext: [String: Any]) {
        DispatchQueue.main.async { self.receiveContext(applicationContext) }
    }
}
