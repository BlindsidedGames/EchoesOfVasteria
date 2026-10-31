import SwiftUI

@main
struct ValidationHost: App {
    @State private var result = "Checking native publish boundary"
    var body: some Scene {
        WindowGroup { Text(result).onAppear { validate() } }
    }
    private func validate() {
        var cases: [String] = []
        do {
            let data = try Data(contentsOf: Bundle.main.url(forResource: "actual-phone-snapshot", withExtension: "json")!)
            EOVWatchPhoneSession.publish(String(data: data, encoding: .utf8)! as NSString)
            guard EOVWatchSnapshotCache.load() == data else { throw Failure.cache }
            cases.append("actual managed snapshot accepted")
            var version = try JSONSerialization.jsonObject(with: data) as! [String: Any]
            version["schemaVersion"] = 2
            let changedVersion = String(data: try JSONSerialization.data(withJSONObject: version), encoding: .utf8)!
            for (name, invalid) in [("malformed retains cache", "{"),
                                    ("unsupported version retains cache", changedVersion),
                                    ("oversized retains cache", String(repeating: " ", count: 32769))] {
                EOVWatchPhoneSession.publish(invalid as NSString)
                guard EOVWatchSnapshotCache.load() == data else { throw Failure.cache }
                cases.append(name)
            }
            result = "Native validation passed"
            try write(["passed": true, "cases": cases])
        } catch {
            result = "Native validation failed"
            try? write(["passed": false, "cases": cases, "error": String(describing: error)])
        }
    }
    private func write(_ value: [String: Any]) throws {
        let url = FileManager.default.urls(for: .documentDirectory, in: .userDomainMask)[0].appendingPathComponent("validation-result.json")
        try JSONSerialization.data(withJSONObject: value, options: [.prettyPrinted, .sortedKeys]).write(to: url)
    }
    enum Failure: Error { case cache }
}
