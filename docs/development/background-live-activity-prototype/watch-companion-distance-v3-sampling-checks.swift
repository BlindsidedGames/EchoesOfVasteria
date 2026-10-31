import Foundation

let root = URL(fileURLWithPath: CommandLine.arguments[1])
var checks = [String]()
func require(_ value: @autoclosure () -> Bool, _ message: String) {
    guard value() else { fatalError(message) }
    checks.append(message)
}
func near(_ value: Double, _ expected: Double) -> Bool { abs(value - expected) < 0.00001 }
func load(_ name: String) throws -> EOVWatchSnapshot {
    try EOVWatchSnapshot.decode(Data(contentsOf: root.appendingPathComponent("watch-companion-distance-v3-managed-" + name + ".json")))
}
let payload = try load("reaping")
let t = payload.timeline!
func at(_ seconds: Double) -> EOVWatchSnapshot.Timeline.Sample { t.sample(at: t.generatedAt + seconds) }
let mid = at(1.5)
require(near(mid.distance, 9.5) && near(mid.cap, 10) && near(mid.resources, 5.25), "midpoint samples movement and gained resources, not endpoint totals")
let before = at(1.999), after = at(2.001), pause = at(4)
require(near(before.distance, 9.999) && before.reaps == 0, "just before reap retains current distance and old cap")
require(after.distance == 0 && near(after.cap, 10.1) && after.resources == 7 && after.reaps == 1 && after.phase == 1,
        "just after reap resets distance, raises cap, increments one reap and retains only7 gained")
require(pause.distance == 0 && pause.resources == 7 && pause.reaps == 1 && pause.phase == 1, "restart pause creates no movement or extra resources")
let restart = at(7.001)
require(near(restart.distance, 2.001) && near(restart.cap, 10.1) && restart.phase == 0, "restart resumes at authored starting distance with effective next cap")
let next = at(15.101), end = at(22)
require(next.distance == 0 && near(next.cap, 10.201) && next.resources == 35 && next.reaps == 2, "successive reap resets using next effective cap and cumulative forecast gain")
require(end.expired && end.resources == 35 && end.reaps == 2 && end.distance == 0, "expiry freezes final estimate without extrapolating")
require(payload.run!.resources == 777 && mid.resources < 777, "current-run total is not added to gained resources")
let originalData = try Data(contentsOf: root.appendingPathComponent("watch-companion-distance-v3-managed-reaping.json"))
var replacement = try JSONSerialization.jsonObject(with: originalData) as! [String: Any]
var replacedTimeline = replacement["timeline"] as! [String: Any]
replacedTimeline["generatedAt"] = t.generatedAt + 15
replacement["timeline"] = replacedTimeline
let newer = try EOVWatchSnapshot.decode(JSONSerialization.data(withJSONObject: replacement)).timeline!
require(near(newer.sample(at: newer.generatedAt + 1.5).resources, 5.25), "replacement forecast supersedes previous projection without accumulation")
let cap = try load("effective-cap").timeline!
require(near(cap.sample(at: cap.generatedAt + 0.5).cap, 100) && near(cap.sample(at: cap.generatedAt + 1.001).cap, 51), "observed buffed cap is replaced by actual post-reap cap")
let hollow = try load("hollow").timeline!
let h = hollow.sample(at: hollow.generatedAt + 10)
require(!hollow.distanceReaping && h.cap == 0 && h.reaps == 0 && h.resources == 35, "Hollow has finite no-cap timeline and no invented distance reap")
let death = try load("death").timeline!
let d = death.sample(at: death.generatedAt + 2.001)
require(d.phase == 1 && d.distance == 0 && d.reaps == 0, "combat death restarts without claiming a reap")
let bounded = try load("bounded").timeline!
require(bounded.truncated && bounded.frames.count == 128 && bounded.horizonSeconds == 128, "frame budget shortens horizon honestly")
for name in ["reaping", "effective-cap", "hollow", "death", "bounded"] {
    let data = try Data(contentsOf: root.appendingPathComponent("watch-companion-distance-v3-managed-" + name + ".json"))
    require(data.count <= 32768, name + " real-six-skill/ten-history wire payload fits32KiB")
}
let capBefore = bounded.sample(at: bounded.generatedAt + 125.999)
let capAfter = bounded.sample(at: bounded.generatedAt + 126.001)
require(near(capBefore.cap, 10124) && near(capAfter.cap, 10126), "time-varying effective cap changes exactly at its observed boundary")
let boundedMid = bounded.sample(at: bounded.generatedAt + 127.5)
require(near(boundedMid.distance, 135.5) && near(boundedMid.resources, 446.25), "arbitrary midpoint within final bounded segment keeps current cap and gained scope")
let old = try EOVWatchSnapshot.decode(Data(contentsOf: root.appendingPathComponent("watch-companion-distance-v3-legacy-snapshot.json")))
require(old.timeline == nil, "old snapshot without timeline remains decodable")
let result: [String: Any] = ["passed": true, "checks": checks, "count": checks.count]
try JSONSerialization.data(withJSONObject: result, options: [.prettyPrinted, .sortedKeys]).write(to: root.appendingPathComponent("watch-companion-distance-v3-native-sampling-result.json"))
print("PASS \(checks.count) independent native wire/sampling checks")
