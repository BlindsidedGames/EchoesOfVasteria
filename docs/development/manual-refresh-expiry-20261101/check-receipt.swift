import Foundation

// Native receipt wire contract. Presentation/sampling and ActivityKit have separate
// owners; these cases protect binding an acknowledgement to the cached departure.
@main
struct CheckReceipt {
    static func main() throws {
        let folder = URL(fileURLWithPath: CommandLine.arguments[1])
        let data = try Data(contentsOf: folder.appendingPathComponent("baseline.json"))
        let value = try EOVWatchSnapshot.decode(data)
        let origin = value.timeline!.generatedAt
        func changed(_ mutate: (inout [String: Any]) -> Void) throws -> EOVWatchSnapshot {
            var object = try JSONSerialization.jsonObject(with: data) as! [String: Any]
            mutate(&object)
            return try EOVWatchSnapshot.decode(JSONSerialization.data(withJSONObject: object))
        }
        var failures = 0, passed = 0
        func check(_ result: Bool, _ name: String) {
            if result { passed += 1; print("PASS " + name) }
            else { failures += 1; print("FAIL " + name) }
        }
        let at = origin + 120
        let estimate = value.backgroundEstimate(at: at)!
        let receipt = EOVWatchSnapshot.EstimateReceipt(mapId: "River", sourceID: estimate.sourceID,
            origin: origin, end: estimate.end, evaluatedAt: at)
        let wire = try JSONEncoder().encode(receipt)
        let decoded = try JSONDecoder().decode(EOVWatchSnapshot.EstimateReceipt.self, from: wire)
        check(decoded.matches(value, at: at), "Encoded receipt binds to the same valid departure")
        let otherSource = try changed { object in
            var forecast = object["forecast"] as! [String: Any]
            forecast["sourceID"] = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
            object["forecast"] = forecast
        }
        check(!decoded.matches(otherSource, at: at), "An old source receipt cannot acknowledge a replacement source")
        let laterOrigin = try changed { object in
            for key in ["timeline", "forecast"] {
                var part = object[key] as! [String: Any]
                part["generatedAt"] = origin + 1
                if let end = part["forecastEnd"] as? Double { part["forecastEnd"] = end + 1 }
                object[key] = part
            }
        }
        check(!decoded.matches(laterOrigin, at: at), "A departure with a different origin rejects the receipt")
        let shorter = try changed { object in object["backgroundLimitSeconds"] = 60 }
        check(!decoded.matches(shorter, at: at), "A changed allowance endpoint rejects the receipt")
        let wrongMap = EOVWatchSnapshot.EstimateReceipt(mapId: "Mines", sourceID: estimate.sourceID,
            origin: origin, end: estimate.end, evaluatedAt: at)
        check(!wrongMap.matches(value, at: at), "A different map cannot acknowledge the cached departure")
        check(!decoded.matches(value, at: at - 61), "Receipt evaluation beyond the accepted clock skew is rejected")
        for field in ["backgroundProgressEnabled", "phoneInBackground"] {
            let disabled = try changed { $0[field] = false }
            check(!decoded.matches(disabled, at: at), "Receipt cannot revive " + field + " disabled data")
        }
        let legacy = try EOVWatchSnapshot.decode(Data(contentsOf: folder.appendingPathComponent("away.json")))
        let legacyOrigin = legacy.timeline!.generatedAt
        let legacyEstimate = legacy.backgroundEstimate(at: legacyOrigin + 10)!
        let legacyReceipt = EOVWatchSnapshot.EstimateReceipt(mapId: "River", sourceID: nil,
            origin: legacyOrigin, end: legacyEstimate.end, evaluatedAt: legacyOrigin + 10)
        check(legacyReceipt.matches(legacy, at: legacyOrigin + 10), "Legacy timeline-only snapshots can acknowledge native estimation without inventing a source")
        print("\(passed) receipt contracts passed; \(failures) failed. No WatchConnectivity delivery claim.")
        if failures > 0 { exit(1) }
    }
}
