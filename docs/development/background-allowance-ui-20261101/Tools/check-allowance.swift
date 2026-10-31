import Foundation

// Native wire/presentation boundary: older payloads omit the policy, a short
// trajectory must not redefine it, and valid high-rate counts must survive.
// No managed estimator replay, transport mock or production test seam.
@main
struct CheckBackgroundAllowance {
    static func main() throws {
        let folder = URL(fileURLWithPath: CommandLine.arguments[1])
        func decode(_ name: String, limit: Any? = nil) throws -> EOVWatchSnapshot {
            let data = try Data(contentsOf: folder.appendingPathComponent(name + ".json"))
            var wire = try JSONSerialization.jsonObject(with: data) as! [String: Any]
            if let limit { wire["backgroundLimitSeconds"] = limit }
            return try EOVWatchSnapshot.decode(JSONSerialization.data(withJSONObject: wire))
        }
        func policy(_ snapshot: EOVWatchSnapshot) throws -> Double? {
            let encoded = try JSONEncoder().encode(snapshot)
            return (try JSONSerialization.jsonObject(with: encoded) as! [String: Any])["backgroundLimitSeconds"] as? Double
        }
        var passed = 0
        func check(_ condition: Bool, _ message: String) {
            precondition(condition, message); passed += 1; print("PASS " + message)
        }
        let high = try decode("allowance-high-rate")
        let now = high.timeline!.generatedAt + 2700
        let view = EOVWatchPresentation(snapshot: high, now: now, reachable: false, cached: true, refreshState: .idle)
        check(view.display.tasks == 405000 && view.display.kills == 405000 && view.display.resources == 405000,
              "540k/hour native trajectory decodes and presents 405k gains at45min")
        let short = try decode("allowance-away")
        check(try policy(short) == 3600 && short.timeline!.horizonSeconds == 300,
              "One-hour allowance survives a shorter five-minute trajectory")
        check(EOVBackgroundAllowance.label(short.backgroundLimitSeconds) == "1h max" &&
              EOVBackgroundAllowance.label(1800) == "30m max" && EOVBackgroundAllowance.label(nil) == nil,
              "Shared phone/Watch label follows allowance and omits a missing value")
        check(try policy(decode("away")) == nil, "Legacy payload does not invent an allowance")
        for invalid: Any in [NSNull(), "3600", true, 0, -1] {
            check(try policy(decode("away", limit: invalid)) == nil,
                  "Invalid optional allowance omitted while valid progress remains: " + String(describing: invalid))
        }
        check(try policy(decode("away", limit: 1800)) == 1800, "Native policy follows transmitted value rather than hardcoded one hour")
        print("\(passed) native allowance/high-rate boundary checks passed.")
    }
}
