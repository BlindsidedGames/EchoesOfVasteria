import Foundation
import ActivityKit
import OSLog

struct RefreshLabAttributes: ActivityAttributes {
    struct ContentState: Codable, Hashable {
        var sampledAt: Date
        var distance: Int
        var resources: Int
        var reaps: Int
        var segment: String
        var refreshCount: Int
        var executor: String
    }
    var origin: Date
    var run: String
    static func sample(origin: Date, now: Date, count: Int, executor: String) -> ContentState {
        let t = max(0, Int(floor(now.timeIntervalSince(origin))))
        // 150/200 at departure. Cap at +2, pause until +4; then 8s move / 2s restart.
        let distance: Int; let reaps: Int; let phase: String
        if t < 2 { distance = 150 + 25*t; reaps = 0; phase = "MOVE" }
        else if t < 4 { distance = 200; reaps = 1; phase = "RESTART" }
        else { let p = (t-4)%10; distance = min(200,p*25); reaps = 1+(t-4)/10; phase = p < 8 ? "MOVE" : "RESTART" }
        return .init(sampledAt: now, distance: distance, resources: 2*t, reaps: reaps, segment: phase, refreshCount: count, executor: executor)
    }
}
func labEvent(_ event: String, _ fields: [String: Any] = [:]) {
    var record = fields
    record["event"] = event; record["at"] = Date().timeIntervalSince1970
    record["pid"] = ProcessInfo.processInfo.processIdentifier
    record["bundle"] = Bundle.main.bundleIdentifier ?? "unknown"
    let data = try! JSONSerialization.data(withJSONObject: record, options: [.sortedKeys])
    let line = String(decoding: data, as: UTF8.self)
    Logger(subsystem: "com.blindsidedgames.timeformatlab", category: "refresh-lab").notice("REFRESH_LAB \(line, privacy: .public)")
    let url = FileManager.default.urls(for: .documentDirectory, in: .userDomainMask)[0].appendingPathComponent("refresh-events.jsonl")
    if !FileManager.default.fileExists(atPath: url.path) { FileManager.default.createFile(atPath: url.path, contents: nil) }
    if let handle = try? FileHandle(forWritingTo: url) { try? handle.seekToEnd(); try? handle.write(contentsOf: data + Data([10])); try? handle.close() }
}
