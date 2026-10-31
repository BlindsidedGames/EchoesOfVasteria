import Foundation

// Compile alongside the production EOVWatchSnapshot.swift. This boundary checks
// cached wire compatibility and gain scope, independent of Unity and ActivityKit UI.
var checked = 0
func require(_ value: @autoclosure () -> Bool, _ message: String) {
    precondition(value(), message); checked += 1
}
let origin = Date().timeIntervalSince1970 - 10
let frames: [[Double]] = [
    [0, 8, 10, 0, 0, 0, 0, 0],
    [2, 10, 10, 7, 0, 0, 2, 1],
    [2, 0, 10.1, 7, 1, 1, 2, 1],
    [7, 0, 10.1, 7, 1, 1, 2, 1],
    [7, 2, 10.1, 7, 1, 0, 2, 1],
    [8, 3, 10.1, 12, 1, 0, 3, 2]
]
func decode(_ rows: [[Double]]) throws -> EOVWatchSnapshot {
    let payload: [String: Any] = [
        "schemaVersion": 1, "generatedAt": origin, "demo": true, "state": "run",
        "backgroundProgressEnabled": true, "phoneInBackground": true,
        "run": ["mapId": "River", "mapTitle": "River", "distance": 8, "cap": 10,
                "healthFraction": 1, "dead": false, "reaped": false, "durationSeconds": 0,
                "tasks": 777, "kills": 888, "resources": 999],
        "timeline": ["schemaVersion": 1, "mapId": "River", "generatedAt": origin,
                     "horizonSeconds": 8, "distanceReaping": true, "truncated": false, "frames": rows],
        "skills": [], "recentRuns": []
    ]
    return try EOVWatchSnapshot.decode(JSONSerialization.data(withJSONObject: payload))
}
let snapshot = try decode(frames), timeline = snapshot.timeline!
let moving = timeline.sample(at: origin + 1.5)
require(moving.distance == 9.5 && moving.cap == 10, "movement samples current distance and effective cap")
require(moving.tasks == 1.5 && moving.kills == 0.75 && moving.resources == 5.25,
        "work counts interpolate gains, not current-run totals or forecast endpoints")
let pause = timeline.sample(at: origin + 4)
require(pause.phase == 1 && pause.distance == 0 && pause.cap == 10.1 && pause.reaps == 1,
        "reap boundary resets distance and uses the next effective cap")
require(pause.tasks == 2 && pause.kills == 1 && pause.resources == 7, "restart pause adds no work")
let restarted = timeline.sample(at: origin + 7.5)
require(restarted.tasks == 2.5 && restarted.kills == 1.5 && restarted.resources == 9.5,
        "gains accumulate across a restart without resetting or paying rewards")
let expired = timeline.sample(at: origin + 100)
require(expired.expired && expired.tasks == 3 && expired.kills == 2 && expired.resources == 12,
        "expired trajectory freezes its final estimate")
require(snapshot.run!.tasks == 777 && snapshot.run!.kills == 888 && snapshot.run!.resources == 999,
        "sampling does not mutate recorded foreground owners")
let legacy = try decode(frames.map { Array($0.prefix(6)) }).timeline!.sample(at: origin + 1.5)
require(legacy.distance == 9.5 && legacy.resources == 5.25 && legacy.tasks == nil && legacy.kills == nil,
        "six-column caches remain readable; unknown counters are unavailable")
func rejected(_ rows: [[Double]], _ message: String) { require((try? decode(rows)) == nil, message) }
var invalid = frames; invalid[1] = Array(invalid[1].prefix(6))
rejected(invalid, "mixed row widths rejected")
invalid = frames; invalid[2][6] = 1
rejected(invalid, "task gain cannot decrease at a reap")
invalid = frames; invalid[3][7] = 0
rejected(invalid, "kill gain cannot decrease")
invalid = frames; invalid[3][6] = 3
rejected(invalid, "restart pause cannot invent task gain")
invalid = frames; invalid[1][6] = 2.1
rejected(invalid, "wire endpoints must contain whole work counts")
invalid = frames; invalid[0][6] = 1
rejected(invalid, "departure counters begin at zero")
print("PASS \(checked) native timeline compatibility and counter checks")
