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
            Group {
                if #available(iOS 18.0, *) { SnapshotFamilyContent(context: context) }
                else { SnapshotPanel(context: context) }
            }
                .activityBackgroundTint(Color(red: 0.08, green: 0.11, blue: 0.12))
                .activitySystemActionForegroundColor(accent)
        } dynamicIsland: { context in
            DynamicIsland {
                DynamicIslandExpandedRegion(.leading) { HeroPortrait(state: context.state, size: 24) }
                DynamicIslandExpandedRegion(.center) {
                    VStack(spacing: 2) {
                        Text(context.state.mapTitle).font(.headline).lineLimit(1)
                        ElapsedStatus(state: context.state)
                    }
                }
                DynamicIslandExpandedRegion(.trailing) { RefreshAction(context: context) }
                DynamicIslandExpandedRegion(.bottom) { SnapshotValues(state: context.state, stale: context.isStale).padding(8) }
            } compactLeading: { HeroPortrait(state: context.state, size: 24) }
              compactTrailing: {
                Text((context.state.status == "live" ? "" : "~") + compactDistance(context.state.distance))
                    .font(.caption2.monospacedDigit()).foregroundStyle(accent)
            } minimal: { PixelArt(name: "EOVCarl", size: 20) }
            .keylineTint(accent)
        }
    }
}
@available(iOS 18.0, *)
private struct SnapshotFamilyContent: View {
    @Environment(\.activityFamily) private var family
    let context: ActivityViewContext<EOVForecastAttributes>
    var body: some View {
        if family == .small {
            HStack(spacing: 4) {
                HeroPortrait(state: context.state, size: 22)
                VStack(alignment: .leading, spacing: 2) {
                    Text(context.state.mapTitle).font(.caption2.bold())
                    Text(context.isStale ? "Estimate ended" :
                        (context.state.status == "live" ? "" : "Est. ") +
                        compactDistance(context.state.distance) +
                        (context.state.cap > 0 ? " / " + compactDistance(context.state.cap) : ""))
                        .font(.caption.monospacedDigit().bold())
                    Text(context.state.resources.rounded(.down).formatted(.number.precision(.fractionLength(0))) +
                        (context.state.status == "live" ? " this run" : " since dep. · \(context.state.reaps) reaps"))
                        .font(.caption2).foregroundStyle(.secondary)
                }.lineLimit(1).minimumScaleFactor(0.6).frame(maxWidth: .infinity, alignment: .leading)
                RefreshAction(context: context, compact: true)
            }.padding(6).foregroundStyle(.white)
        } else { SnapshotPanel(context: context) }
    }
}
private struct SnapshotPanel: View {
    let context: ActivityViewContext<EOVForecastAttributes>
    var body: some View {
        VStack(alignment: .leading, spacing: 7) {
            HStack(spacing: 10) {
                HeroPortrait(state: context.state, size: 32)
                VStack(alignment: .leading, spacing: 2) {
                    Text(context.state.mapTitle).font(.headline).lineLimit(1)
                    ElapsedStatus(state: context.state)
                }
                Spacer(minLength: 0)
                RefreshAction(context: context)
            }
            SnapshotValues(state: context.state, stale: context.isStale)
        }.padding(.horizontal, 14).padding(.vertical, 10).foregroundStyle(.white)
    }
}
private struct ElapsedStatus: View {
    let state: EOVForecastAttributes.ContentState
    var body: some View {
        if let origin = state.backgroundOrigin {
            HStack(spacing: 4) {
                Text("Elapsed").foregroundStyle(.secondary)
                // The system can replace seconds with words on a dimmed display.
                Text("00:00:00").hidden().frame(width: 84, alignment: .leading).overlay(alignment: .leading) {
                    Text(timerInterval: Date(timeIntervalSince1970: origin)...Date(timeIntervalSince1970: origin + 8 * 3600),
                         countsDown: false, showsHours: true)
                        .lineLimit(1).minimumScaleFactor(0.8)
                }.monospacedDigit().fixedSize(horizontal: true, vertical: false)
            }.font(.caption)
        } else { Text("Live · this run").font(.caption).foregroundStyle(.secondary) }
    }
}
private struct SnapshotValues: View {
    let state: EOVForecastAttributes.ContentState
    let stale: Bool
    private var metricScope: String { state.backgroundOrigin == nil ? "this run" : "since departure" }
    var body: some View {
        VStack(alignment: .leading, spacing: 7) {
            VStack(spacing: 4) {
                HStack {
                    Text(distance(state.distance) + (state.cap > 0 ? " / " + distance(state.cap) : ""))
                        .font(.subheadline.monospacedDigit().bold()).lineLimit(1).minimumScaleFactor(0.7)
                    Spacer(minLength: 4)
                    Text(stale ? "Estimate ended" : state.status == "live" ? "Live" : state.status == "waiting" ? "Waiting for you" : state.status == "restarting" ? "Restarting · est." : "Estimated")
                        .font(.caption2).foregroundStyle(.secondary)
                }
                if state.cap > 0 {
                    ProgressView(value: min(state.cap, state.distance), total: state.cap)
                        .tint(Color(red: 0.94, green: 0.75, blue: 0.42))
                }
            }
            HStack(spacing: 4) {
                ActivityMetric(art: "EOVTask", count: state.tasks, label: "Tasks " + metricScope)
                ActivityMetric(art: "EOVKill", count: state.kills, label: "Kills " + metricScope)
                ActivityMetric(art: "EOVChest", count: state.resources, label: "Resources " + metricScope,
                               prefix: state.backgroundOrigin == nil ? "" : "+")
                ActivityMetric(art: state.reaps.isMultiple(of: 2) ? "EOVCarl" : "EOVCarlSwing",
                               count: Double(state.reaps), label: "Reaps " + metricScope,
                               movementRevision: state.reapRevision)
            }.foregroundStyle(.secondary)
        }
    }
}
private struct ActivityMetric: View {
    let art: String
    let count: Double?
    let label: String
    var prefix = ""
    var movementRevision: UInt? = nil
    private var value: String {
        count.map { prefix + $0.rounded(.down).formatted(.number.precision(.fractionLength(0))) } ?? "—"
    }
    var body: some View {
        HStack(spacing: 4) {
            if let movementRevision {
                ReapIcon(name: art, revision: movementRevision)
            } else { PixelArt(name: art, size: 16) }
            Text(value).lineLimit(1).minimumScaleFactor(0.7)
        }.font(.caption.monospacedDigit())
            .frame(maxWidth: .infinity, alignment: .leading)
            .accessibilityElement(children: .ignore)
            .accessibilityLabel(label)
            .accessibilityValue(value)
    }
}
private struct HeroPortrait: View {
    let state: EOVForecastAttributes.ContentState
    let size: CGFloat
    @Environment(\.isLuminanceReduced) private var dimmed
    @Environment(\.accessibilityReduceMotion) private var reduceMotion
    var body: some View {
        Group {
            if let layers = state.portraitLayers, !layers.isEmpty {
                ZStack {
                    ForEach(Array(layers.enumerated()), id: \.offset) { _, name in
                        Image(name).resizable().interpolation(.none).scaledToFit()
                            .frame(width: size * 2, height: size * 2)
                    }
                }.frame(width: size, height: size).clipped()
            } else { PixelArt(name: "EOVHero", size: size) }
        }
        .id(state.refreshRevision ?? 0)
        .transition(dimmed || reduceMotion ? .identity :
            .asymmetric(insertion: .offset(y: 3).combined(with: .opacity), removal: .opacity))
        .animation(updateAnimation(disabled: dimmed || reduceMotion), value: state.refreshRevision)
        .accessibilityElement(children: .ignore).accessibilityLabel("Your character")
    }
}
private struct ReapIcon: View {
    let name: String
    let revision: UInt
    @Environment(\.isLuminanceReduced) private var dimmed
    @Environment(\.accessibilityReduceMotion) private var reduceMotion
    var body: some View {
        PixelArt(name: name, size: 16)
            .id(revision)
            .transition(dimmed || reduceMotion ? .identity :
                .asymmetric(insertion: .offset(x: 3).combined(with: .opacity), removal: .opacity))
            .animation(updateAnimation(disabled: dimmed || reduceMotion), value: revision)
    }
}
private func updateAnimation(disabled: Bool) -> Animation? {
    guard !disabled else { return nil }
    if #available(iOS 17.0, *) { return .spring(duration: 0.35, bounce: 0.2) }
    return nil
}
private struct RefreshAction: View {
    let context: ActivityViewContext<EOVForecastAttributes>
    var compact = false
    var body: some View {
        if context.state.canRefresh && !context.isStale {
            if #available(iOS 17.0, *) {
                Button(intent: EOVRefreshIntent(activityID: context.activityID, sourceID: context.state.sourceID)) {
                    Image(systemName: "arrow.clockwise").font(.system(size: 16, weight: .semibold))
                        .frame(width: 30, height: 30)
                        .background(.white.opacity(0.05), in: RoundedRectangle(cornerRadius: 8))
                        .overlay(RoundedRectangle(cornerRadius: 8).stroke(.white.opacity(0.12)))
                        .frame(minWidth: compact ? 32 : 44, minHeight: 44)
                }.buttonStyle(.plain).tint(Color(red: 0.94, green: 0.75, blue: 0.42))
                    .accessibilityLabel("Refresh outing estimate")
            } else { Text("Open Echoes to refresh").font(.caption2).foregroundStyle(.secondary) }
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
