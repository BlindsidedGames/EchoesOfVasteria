import SwiftUI

// This executable is a separate fixture adapter. None of these arguments,
// fixtures, transport substitutions or frozen clocks are part of the Watch app.
@main
struct EOVWatchIntegrationPreview: App {
    @State private var screen: EOVWatchScreen
    @State private var refreshState = EOVWatchRefreshState.idle
    private let snapshot: EOVWatchSnapshot?
    private let clock: Double
    private let largeText: Bool
    private let reachable: Bool
    private let cached: Bool
    init() {
        func arg(_ key: String) -> String? { ProcessInfo.processInfo.arguments.first { $0.hasPrefix("--" + key + "=") }?.split(separator: "=", maxSplits: 1).last.map(String.init) }
        let state = arg("state") ?? "live"
        let file = state == "empty" ? "live" : state
        let url = Bundle.main.url(forResource: file, withExtension: "json", subdirectory: "Fixtures") ?? Bundle.main.url(forResource: file, withExtension: "json")
        let data = url.flatMap { try? Data(contentsOf: $0) }.flatMap { try? EOVWatchSnapshot.decode($0) }
        snapshot = state == "empty" ? nil : data
        let origin = data?.timeline?.generatedAt ?? data?.generatedAt ?? 0
        clock = origin + (arg("offset").flatMap(Double.init) ?? (state == "stale" ? 7200 : state == "restart" ? 110 : state == "away" || state == "large-away" ? 85 : 15))
        _screen = State(initialValue: EOVWatchScreen(rawValue: arg("screen") ?? "automatic") ?? .automatic)
        largeText = arg("text") == "large"
        reachable = state != "offline" && state != "stale" && state != "empty"
        cached = !reachable
    }
    var body: some Scene {
        WindowGroup {
            NavigationStack {
                EOVWatchShell(presentation: .init(snapshot: snapshot, now: clock, reachable: reachable, cached: cached, refreshState: refreshState), screen: $screen) {
                    // Exercise presentation states only. This is not WatchConnectivity evidence.
                    refreshState = .requesting
                    Task { @MainActor in
                        try? await Task.sleep(for: .milliseconds(350))
                        refreshState = reachable ? .queued : .unavailable
                    }
                }
            }.tint(EOVWatchPalette.gold).preferredColorScheme(.dark)
                .environment(\.dynamicTypeSize, largeText ? .accessibility1 : .large)
        }
    }
}
