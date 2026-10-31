import SwiftUI

@main
struct WatchRenderingHarness: App {
    private let start = Date().addingTimeInterval(-15)
    @State private var expired = false
    private var mode: String {
        let args = ProcessInfo.processInfo.arguments
        if args.contains("-ended") { return "ended" }
        if args.contains("-stale") { return "stale" }
        return "active"
    }

    var body: some Scene {
        WindowGroup {
            VStack(spacing: 5) {
                Text("DIRECT UI HARNESS").font(.system(size: 9, weight: .semibold))
                    .foregroundStyle(.secondary)
                EOVWatchForecastView(mapTitle: "River · demo", projectedDistance: 1800,
                    projectedReaps: 2, timeRange: start...start.addingTimeInterval(60),
                    stale: mode == "stale" || expired, ended: mode == "ended")
                .frame(width: ProcessInfo.processInfo.arguments.contains("-large") ? 194 : 176,
                       height: ProcessInfo.processInfo.arguments.contains("-large") ? 80.5 : 72.5)
                .background(Color(red: 0.08, green: 0.11, blue: 0.12), in: RoundedRectangle(cornerRadius: 16))
                .task {
                    // Single foreground harness deadline supplies the stale context;
                    // timer text/ring remain OS-driven and production has no task.
                    let remaining = max(0, start.addingTimeInterval(60).timeIntervalSinceNow)
                    try? await Task.sleep(nanoseconds: UInt64(remaining * 1_000_000_000))
                    if !Task.isCancelled { expired = true }
                }
                Text("Synthetic forecast\nSame production small view")
                    .font(.system(size: 9)).foregroundStyle(.secondary)
                    .multilineTextAlignment(.center)
            }
            .padding(.horizontal, 4)
        }
    }
}
