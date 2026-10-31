import Foundation

struct EOVWatchSnapshot: Codable {
    let schemaVersion: Int
    let generatedAt: Double
    let demo: Bool
    let state: String
    let backgroundProgressEnabled: Bool
    let run: Run?
    let forecast: Forecast?
    let skills: [Skill]
    let recentRuns: [RecentRun]

    struct Run: Codable {
        let mapId: String
        let mapTitle: String
        let distance: Double
        let cap: Double
        let healthFraction: Double
        let dead: Bool
        let reaped: Bool
        let durationSeconds: Double
        let tasks: Int
        let kills: Int
        let resources: Double
    }
    struct Skill: Codable, Identifiable {
        let id: String
        let name: String
        let level: Int
    }
    struct RecentRun: Codable, Identifiable {
        let runNumber: Int
        let mapId: String
        let mapTitle: String
        let durationSeconds: Double
        let distance: Double
        let tasks: Int
        let kills: Int
        let resources: Double
        let result: String
        let estimated: Bool
        var id: Int { runNumber }
    }
    // Mirrors the established ActivityKit JSON without importing ActivityKit on Watch.
    struct Forecast: Codable {
        let schemaVersion: Int
        let mapId: String
        let mapTitle: String
        let sampleCount: Int
        let generatedAt: Double
        let currentDistance: Double
        let projectedDistance: Double
        let reapCap: Double
        let projectedReaps: Int
        let projectedTasks: Int
        let projectedKills: Int
        let projectedResources: Double
        let resourceSummary: String?
        let forecastEnd: Double
        let status: String
        var timeRange: ClosedRange<Date> {
            Date(timeIntervalSince1970: generatedAt)...Date(timeIntervalSince1970: forecastEnd)
        }
    }

    enum Invalid: Error { case snapshot }
    static func decode(_ data: Data) throws -> EOVWatchSnapshot {
        guard !data.isEmpty, data.count <= 32 * 1024 else { throw Invalid.snapshot }
        let value = try JSONDecoder().decode(Self.self, from: data)
        try value.validate()
        return value
    }
    private func validate() throws {
        func title(_ s: String) -> Bool { !s.isEmpty && s.count <= 120 }
        func identifier(_ s: String) -> Bool { !s.isEmpty && s.count <= 80 }
        func numbers(_ values: Double...) -> Bool { values.allSatisfy { $0.isFinite && $0 >= 0 } }
        guard schemaVersion == 1, generatedAt.isFinite, generatedAt > 0,
              ["run", "town", "loading"].contains(state), skills.count <= 32, recentRuns.count <= 10,
              Set(skills.map(\.id)).count == skills.count,
              Set(recentRuns.map(\.runNumber)).count == recentRuns.count else { throw Invalid.snapshot }
        if let r = run {
            guard identifier(r.mapId), title(r.mapTitle),
                  numbers(r.distance, r.cap, r.healthFraction, r.durationSeconds, r.resources),
                  r.healthFraction <= 1, r.tasks >= 0, r.kills >= 0 else { throw Invalid.snapshot }
        }
        for s in skills {
            guard identifier(s.id), title(s.name), s.level >= 0 else { throw Invalid.snapshot }
        }
        for r in recentRuns {
            guard r.runNumber >= 0, identifier(r.mapId), title(r.mapTitle),
                  numbers(r.durationSeconds, r.distance, r.resources), r.tasks >= 0, r.kills >= 0,
                  ["reaped", "died", "abandoned", "completed"].contains(r.result) else { throw Invalid.snapshot }
        }
        if let f = forecast {
            guard f.schemaVersion == 1, identifier(f.mapId), !f.mapTitle.isEmpty, f.mapTitle.count <= 80, f.sampleCount >= 0,
                  numbers(f.generatedAt, f.currentDistance, f.projectedDistance, f.reapCap,
                          f.projectedResources, f.forecastEnd), f.generatedAt > 0,
                  f.forecastEnd > f.generatedAt, f.forecastEnd - f.generatedAt <= 8 * 3600,
                  f.generatedAt <= Date().timeIntervalSince1970 + 60,
                  f.projectedReaps >= 0, f.projectedTasks >= 0, f.projectedKills >= 0,
                  (f.resourceSummary?.count ?? 0) <= 200,
                  ["forecast", "insufficientHistory", "ended"].contains(f.status) else { throw Invalid.snapshot }
        }
    }
}

enum EOVWatchSnapshotCache {
    static var url: URL {
        FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0]
            .appendingPathComponent("EOVWatchCompanion", isDirectory: true)
            .appendingPathComponent("latest-v1.json")
    }
    static func load() -> Data? {
        guard let data = try? Data(contentsOf: url), (try? EOVWatchSnapshot.decode(data)) != nil else { return nil }
        return data
    }
    static func save(_ data: Data) {
        do {
            try FileManager.default.createDirectory(at: url.deletingLastPathComponent(), withIntermediateDirectories: true)
            try data.write(to: url, options: .atomic)
        } catch { NSLog("[EOVWatch] snapshot cache write failed: %@", String(describing: error)) }
    }
}
