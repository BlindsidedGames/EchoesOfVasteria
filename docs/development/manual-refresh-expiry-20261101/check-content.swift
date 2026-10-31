import Foundation
import ActivityKit

@main
struct CheckContent {
    static func main() throws {
        var failures = 0, passed = 0
        func check(_ value: Bool, _ name: String) {
            if value { passed += 1; print("PASS " + name) }
            else { failures += 1; print("FAIL " + name) }
        }
        var state = EOVForecastAttributes.ContentState(sourceID: String(repeating: "a", count: 32),
            mapTitle: "River", sampledAt: 105, backgroundOrigin: 100, forecastEnd: 160,
            distance: 5, cap: 20, resources: 7, tasks: 2, kills: 1, reaps: 0, status: "estimate",
            backgroundLimitSeconds: 3600,
            terminal: .init(distance: 20, cap: 21, resources: 99, tasks: 40, kills: 10, reaps: 1, status: "estimateEnded"))
        check(state.displayed(stale: false) == state, "Current display retains refreshed values before the system stale flag")
        let stale = state.displayed(stale: true)
        check(stale.distance == 20 && stale.cap == 21 && stale.resources == 99 && stale.tasks == 40 && stale.kills == 10 && stale.reaps == 1 && stale.status == "estimateEnded" && !stale.canRefresh,
              "System stale display selects every preloaded terminal scalar and retires Refresh")
        check(stale.sampledAt == 105 && stale.sourceID == state.sourceID && stale.backgroundOrigin == 100 && stale.clockEnd == 3700,
              "Terminal display preserves source timestamp and allowance clock distinct from coverage")
        state.status = "live"; state.backgroundOrigin = nil
        check(state.displayed(stale: true) == state, "Stale presentation cannot substitute forecast gains into live totals")
        state.status = "estimate"; state.backgroundOrigin = 100; state.terminal?.resources = -1
        let invalid = state.displayed(stale: true)
        check(invalid.resources == 7 && invalid.distance == 5 && invalid.status == "estimateEnded" && !invalid.canRefresh,
              "Malformed optional terminal keeps valid scalars and retires stale Refresh safely")
        var legacyObject = try JSONSerialization.jsonObject(with: JSONEncoder().encode(state)) as! [String: Any]
        legacyObject.removeValue(forKey: "terminal"); legacyObject.removeValue(forKey: "backgroundLimitSeconds")
        let legacy = try JSONDecoder().decode(EOVForecastAttributes.ContentState.self, from: JSONSerialization.data(withJSONObject: legacyObject))
        check(legacy.clockEnd == 160 && legacy.displayed(stale: true).resources == 7 && legacy.displayed(stale: true).status == "estimateEnded",
              "Legacy Activity state without endpoint or allowance decodes and uses known coverage")
        for status in ["limitReached", "estimateEnded", "waiting"] {
            state.terminal = .init(distance: 20, cap: 21, resources: 99, tasks: nil, kills: nil, reaps: 1, status: status)
            let final = state.displayed(stale: true)
            check(final.status == status && final.tasks == nil && final.kills == nil && !final.canRefresh,
                  "Terminal " + status + " retains its reason and unknown legacy counters")
        }
        print("\(passed) Activity content contracts passed; \(failures) failed. No system rendering claim.")
        if failures > 0 { exit(1) }
    }
}
