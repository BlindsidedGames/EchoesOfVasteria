import Foundation
@main
struct ValidateFixtures {
    static func main() throws {
        let folder = URL(fileURLWithPath: CommandLine.arguments[1])
        for file in try FileManager.default.contentsOfDirectory(at: folder, includingPropertiesForKeys: nil).filter({ $0.pathExtension == "json" }).sorted(by: { $0.path < $1.path }) {
            let snapshot = try EOVWatchSnapshot.decode(Data(contentsOf: file))
            print("VALID \(file.lastPathComponent): \(snapshot.recentRuns.count) retained runs, \(snapshot.skills.count) skills")
        }
        func fixture(_ name: String) throws -> EOVWatchSnapshot { try EOVWatchSnapshot.decode(Data(contentsOf: folder.appendingPathComponent(name + ".json"))) }
        let away = try fixture("away").timeline!
        for second in [100.0, 110.0, 119.0] {
            let point = away.sample(at: away.generatedAt + second)
            precondition(point.phase == 1 && point.distance == 0 && point.resources == 75 && point.tasks == 40 && point.kills == 8 && point.reaps == 1 && point.cap == 214.12)
        }
        let end = away.sample(at: away.generatedAt + 300)
        let expired = away.sample(at: away.generatedAt + 7200)
        precondition(end.resources == expired.resources && end.tasks == expired.tasks && end.kills == expired.kills && end.distance == expired.distance && expired.expired && expired.phase == 2)
        let beyond = try fixture("beyond-history")
        let historyBest = beyond.recentRuns.filter { $0.mapId == "Mines" }.map(\.distance).max()!
        precondition(historyBest == 2550 && beyond.run!.distance == 5200 && beyond.run!.cap == 38000)
        let noReap = try fixture("no-reap").run!
        precondition(noReap.distanceReaping == false && noReap.cap == 0)
        print("PASS restart counters frozen, +1% cap, terminal/expiry frozen, beyond-history actual cap, disabled distance reap")
        print("This validates fixture presentation contracts, not the managed estimator or WatchConnectivity transport.")
    }
}
