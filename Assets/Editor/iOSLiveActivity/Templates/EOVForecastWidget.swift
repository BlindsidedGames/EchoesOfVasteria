import ActivityKit
import SwiftUI
import WidgetKit
import AppIntents

@main
@MainActor
enum EOVForecastWidgetEntry {
    static func main() {
        if #available(iOS 18.0, *) { EOVSupplementalWidgetBundle.main() }
        else { EOVForecastWidgetBundle.main() }
    }
}
struct EOVForecastWidgetBundle: WidgetBundle {
    var body: some Widget { EOVForecastWidget() }
}
@available(iOS 18.0, *)
private struct EOVSupplementalWidgetBundle: WidgetBundle {
    var body: some Widget { EOVSupplementalForecastWidget() }
}
@available(iOS 18.0, *)
private struct EOVSupplementalForecastWidget: Widget {
    var body: some WidgetConfiguration { EOVForecastWidget().body.supplementalActivityFamilies([.small, .medium]) }
}
struct EOVForecastWidget: Widget {
    private let accent = Color(red: 0.94, green: 0.75, blue: 0.42)
    var body: some WidgetConfiguration {
        ActivityConfiguration(for: EOVForecastAttributes.self) { context in
            SnapshotPanel(context: context)
                .activityBackgroundTint(Color(red: 0.08, green: 0.11, blue: 0.12))
                .activitySystemActionForegroundColor(accent)
        } dynamicIsland: { context in
            DynamicIsland {
                DynamicIslandExpandedRegion(.leading) { PixelArt(name: "EOVHero", size: 24) }
                DynamicIslandExpandedRegion(.trailing) { RefreshAction(context: context) }
                DynamicIslandExpandedRegion(.bottom) { SnapshotValues(state: context.state, stale: context.isStale).padding(8) }
            } compactLeading: { PixelArt(name: "EOVHero", size: 24) }
              compactTrailing: {
                Text((context.state.status == "live" ? "" : "~") + compactDistance(context.state.distance))
                    .font(.caption2.monospacedDigit()).foregroundStyle(accent)
            } minimal: { PixelArt(name: "EOVCarl", size: 20) }
            .keylineTint(accent)
        }
    }
}
private struct SnapshotPanel: View {
    let context: ActivityViewContext<EOVForecastAttributes>
    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            HStack(spacing: 8) {
                PixelArt(name: "EOVHero", size: 26)
                Text(context.state.mapTitle).font(.headline).lineLimit(1)
                Spacer(minLength: 0)
                RefreshAction(context: context)
            }
            SnapshotValues(state: context.state, stale: context.isStale)
        }.padding(12).foregroundStyle(.white)
    }
}
private struct SnapshotValues: View {
    let state: EOVForecastAttributes.ContentState
    let stale: Bool
    var body: some View {
        VStack(alignment: .leading, spacing: 5) {
            HStack {
                Text(distance(state.distance) + (state.cap > 0 ? " / " + distance(state.cap) : ""))
                    .font(.system(.subheadline, design: .rounded).bold()).lineLimit(1).minimumScaleFactor(0.7)
                Spacer(minLength: 4)
                Text(stale ? "Estimate ended" : state.status == "live" ? "Live" : state.status == "waiting" ? "Waiting for you" : state.status == "restarting" ? "Restarting · est." : "Estimated")
                    .font(.caption2).foregroundStyle(.secondary)
            }
            if state.cap > 0 {
                // Snapshot distance, not a date-driven bar that would cross an unmodelled restart.
                ProgressView(value: min(state.cap, state.distance), total: state.cap)
                    .tint(Color(red: 0.94, green: 0.75, blue: 0.42))
            }
            HStack(spacing: 6) {
                PixelArt(name: "EOVResource", size: 16)
                Text(state.resources.rounded(.down).formatted(.number.precision(.fractionLength(0))))
                Text(state.status == "live" ? "this run" : "since departure").foregroundStyle(.secondary)
                Spacer(minLength: 2)
                if state.status != "live" {
                    PixelArt(name: "EOVCarl", size: 18)
                    Text("\(state.reaps) " + (state.reaps == 1 ? "reap" : "reaps"))
                }
            }.font(.caption.monospacedDigit()).lineLimit(1).minimumScaleFactor(0.7)
            HStack(spacing: 3) {
                Text("Updated")
                Text(Date(timeIntervalSince1970: state.sampledAt), style: .relative)
                Text("ago")
            }.font(.caption2).foregroundStyle(.secondary)
        }
    }
}
private struct RefreshAction: View {
    let context: ActivityViewContext<EOVForecastAttributes>
    var body: some View {
        if context.state.canRefresh && !context.isStale {
            if #available(iOS 17.0, *) {
                Button(intent: EOVRefreshIntent(activityID: context.activityID, sourceID: context.state.sourceID)) {
                    Label("Refresh", systemImage: "arrow.clockwise").font(.caption.bold())
                }.buttonStyle(.bordered).accessibilityLabel("Refresh outing estimate")
            } else {
                Text("Open Echoes to refresh").font(.caption2).foregroundStyle(.secondary)
            }
        }
    }
}
private struct PixelArt: View {
    let name: String
    let size: CGFloat
    var body: some View { Image(name).resizable().interpolation(.none).scaledToFit().frame(width: size, height: size).accessibilityHidden(true) }
}
private func distance(_ value: Double) -> String { value.formatted(.number.precision(.fractionLength(0))) + " m" }
private func compactDistance(_ value: Double) -> String { value >= 1000 ? String(format: "%.1fk", value / 1000) : String(format: "%.0fm", value) }
