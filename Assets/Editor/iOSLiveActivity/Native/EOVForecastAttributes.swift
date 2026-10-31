import ActivityKit
import Foundation

@available(iOS 16.2, *)
public struct EOVForecastAttributes: ActivityAttributes {
    public struct ContentState: Codable, Hashable {
        public var sourceID: String
        public var mapTitle: String
        public var sampledAt: Double
        public var backgroundOrigin: Double?
        public var forecastEnd: Double
        public var distance: Double
        public var cap: Double
        public var resources: Double
        public var tasks: Double?
        public var kills: Double?
        public var reaps: Int
        public var status: String
        public var portraitLayers: [String]? = nil
        public var refreshRevision: UInt? = nil
        public var reapRevision: UInt? = nil
        public var backgroundLimitSeconds: Double? = nil
        public var terminal: Endpoint? = nil
        public var canRefresh: Bool { status == "estimate" || status == "restarting" }
        public var statusLabel: String {
            switch status {
            case "live": return "Live"
            case "waiting": return "Waiting for you"
            case "restarting": return "Restarting · est."
            case "limitReached": return "Limit reached"
            case "estimateEnded": return "Estimate ended"
            default: return "Estimated"
            }
        }
        public var clockEnd: Double {
            guard let origin = backgroundOrigin else { return forecastEnd }
            if let seconds = backgroundLimitSeconds, seconds.isFinite, seconds > 0 { return origin + seconds }
            return max(origin, forecastEnd)
        }
        // isStale is supplied by the system. No date polling or custom timer is
        // needed to switch every presentation to the already-computed endpoint.
        public func displayed(stale: Bool) -> Self {
            guard stale, backgroundOrigin != nil else { return self }
            var next = self
            next.status = "estimateEnded"
            if let terminal, terminal.isValid {
                next.distance = terminal.distance; next.cap = terminal.cap
                next.resources = terminal.resources; next.tasks = terminal.tasks
                next.kills = terminal.kills; next.reaps = terminal.reaps
                next.status = terminal.status
            }
            return next
        }
    }
    public struct Endpoint: Codable, Hashable {
        public var distance: Double
        public var cap: Double
        public var resources: Double
        public var tasks: Double?
        public var kills: Double?
        public var reaps: Int
        public var status: String
        public var isValid: Bool {
            [distance, cap, resources].allSatisfy { $0.isFinite && $0 >= 0 } &&
                [tasks, kills].compactMap { $0 }.allSatisfy { $0.isFinite && $0 >= 0 } && reaps >= 0 &&
                ["limitReached", "estimateEnded", "waiting"].contains(status)
        }
    }
    public var mapId: String
}
