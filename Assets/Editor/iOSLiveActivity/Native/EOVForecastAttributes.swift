import ActivityKit
import Foundation

@available(iOS 16.2, *)
public struct EOVForecastAttributes: ActivityAttributes {
    public struct ContentState: Codable, Hashable {
        public var schemaVersion: Int
        public var mapId: String
        public var mapTitle: String
        public var sampleCount: Int
        public var generatedAt: Double
        public var currentDistance: Double
        public var projectedDistance: Double
        public var reapCap: Double
        public var projectedReaps: Int
        public var projectedTasks: Int
        public var projectedKills: Int
        public var projectedResources: Double
        public var resourceSummary: String?
        public var forecastEnd: Double
        public var status: String

        public var timeRange: ClosedRange<Date> {
            Date(timeIntervalSince1970: generatedAt)...Date(timeIntervalSince1970: forecastEnd)
        }

        public func validate() throws {
            guard schemaVersion == 1, !mapId.isEmpty, mapId.count <= 80,
                  !mapTitle.isEmpty, mapTitle.count <= 80, sampleCount >= 0,
                  [generatedAt, currentDistance, projectedDistance, reapCap, forecastEnd, projectedResources].allSatisfy({ $0.isFinite }),
                  generatedAt > 0, forecastEnd > generatedAt,
                  forecastEnd - generatedAt <= 8 * 60 * 60,
                  generatedAt <= Date().timeIntervalSince1970 + 60,
                  currentDistance >= 0, projectedDistance >= 0, reapCap >= 0,
                  projectedReaps >= 0, projectedTasks >= 0, projectedKills >= 0, projectedResources >= 0,
                  (resourceSummary?.count ?? 0) <= 200,
                  ["forecast", "insufficientHistory", "ended"].contains(status)
            else { throw ForecastError.invalidPayload }
        }
    }

    public enum ForecastError: Error { case invalidPayload, payloadTooLarge }
    public var mapId: String
}
