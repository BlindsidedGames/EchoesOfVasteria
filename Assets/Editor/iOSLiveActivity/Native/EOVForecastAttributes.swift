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
        public var canRefresh: Bool { status == "estimate" || status == "restarting" }
    }
    public var mapId: String
}
