import Foundation

// Owns only the new Watch presentation boundary. Existing decoder/sampler and
// managed-estimator tests retain their own contracts; this adds no production hooks.
@main
struct CheckWatchPresentation {
    static func main() throws {
        let folder = URL(fileURLWithPath: CommandLine.arguments[1])
        func snapshot(_ file: String, mutate: ((inout [String: Any]) -> Void)? = nil) throws -> EOVWatchSnapshot {
            let data = try Data(contentsOf: folder.appendingPathComponent(file + ".json"))
            var object = try JSONSerialization.jsonObject(with: data) as! [String: Any]
            mutate?(&object)
            return try EOVWatchSnapshot.decode(JSONSerialization.data(withJSONObject: object))
        }
        func model(_ snapshot: EOVWatchSnapshot?, _ offset: Double = 15, cached: Bool = false, reachable: Bool = true) -> EOVWatchPresentation {
            .init(snapshot: snapshot, now: (snapshot?.timeline?.generatedAt ?? snapshot?.generatedAt ?? 0) + offset, reachable: reachable, cached: cached, refreshState: .idle)
        }
        var passed = 0
        func check(_ condition: Bool, _ name: String) {
            precondition(condition, name); passed += 1; print("PASS " + name)
        }
        let live = try snapshot("live")
        let foreground = model(live)
        check(foreground.resolved(.automatic) == .dial && foreground.away == nil && foreground.heroCanAnimate, "Foreground opens dial using a current phone snapshot")
        let away = model(try snapshot("away"), 85)
        check(away.resolved(.automatic) == .away && away.display.estimated && abs(away.display.resources - 63.75) < 0.001 && abs(away.phone!.resources - 16.7454999685) < 0.001 && away.display.health == away.phone!.healthFraction && !away.heroCanAnimate, "Away UI shows gain-only resources and never forecasts phone health")
        check(!model(live, cached: true).heroCanAnimate && !model(live, reachable: false).heroCanAnimate && !model(live, 46).heroCanAnimate, "Cached, unavailable or old foreground data cannot animate as current")
        let disabled = try snapshot("away") { value in value["backgroundProgressEnabled"] = false }
        check(model(disabled).resolved(.automatic) == .dial && model(disabled).away == nil, "Opted-out snapshots cannot select an away estimate")
        let legacy = try snapshot("away") { value in
            var timeline = value["timeline"] as! [String: Any]
            timeline["frames"] = (timeline["frames"] as! [[Double]]).map { Array($0.prefix(6)) }; value["timeline"] = timeline
        }
        check(model(legacy, 85).display.tasks == nil && model(legacy, 85).display.kills == nil, "Legacy unknown task/kill gains stay unknown at the view boundary")
        let mismatch = try snapshot("away") { value in var t = value["timeline"] as! [String: Any]; t["mapId"] = "Mines"; value["timeline"] = t }
        check(model(mismatch).away == nil && model(mismatch).resolved(.automatic) == .dial, "A different map's trajectory cannot replace phone data")
        let short = try snapshot("baseline") { value in var f = value["forecast"] as! [String: Any]; f["forecastEnd"] = (f["generatedAt"] as! Double) + 60; value["forecast"] = f }
        let first = model(short, 120), later = model(short, 180)
        check(first.away!.expired && later.away!.expired && first.display.resources == later.display.resources && first.display.distance == later.display.distance && later.resolved(.automatic) == .away, "Earlier forecast expiry freezes the UI without silently reverting to totals")
        let future = model(try snapshot("away"), -1)
        check(future.away == nil, "Watch time before the forecast origin does not sample future gains")
        for name in ["dead", "reaped"] {
            let terminal = model(try snapshot(name))
            check(terminal.away == nil && !terminal.display.active && !terminal.heroCanAnimate, "Terminal " + name + " remains static")
        }
        let town = model(try snapshot("town")), empty = model(nil)
        check(town.phone == nil && town.resolved(.automatic) == .dial && town.snapshot?.skills.isEmpty == false && empty.phone == nil && !empty.heroCanAnimate, "Town retains skills/history; missing run data stays empty")
        print("\(passed) Watch presentation contracts passed. No transport or managed-estimator claim.")
    }
}
