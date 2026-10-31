import SwiftUI

@main
struct EOVWatchCompanionApp: App {
    @StateObject private var store = EOVWatchCompanionStore()
    @State private var screen = EOVWatchScreen.automatic
    var body: some Scene {
        WindowGroup {
            NavigationStack {
                TimelineView(.periodic(from: .now, by: 1)) { context in
                    EOVWatchShell(presentation: .init(snapshot: store.snapshot,
                        now: context.date.timeIntervalSince1970, reachable: store.reachable,
                        cached: store.cached, refreshState: store.refreshState),
                        screen: $screen, refresh: store.refresh)
                }
            }.tint(EOVWatchPalette.gold).preferredColorScheme(.dark)
        }
    }
}
