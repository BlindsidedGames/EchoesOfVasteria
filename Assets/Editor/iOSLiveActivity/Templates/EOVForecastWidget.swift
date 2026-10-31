import ActivityKit
import SwiftUI
import WidgetKit

@main
@MainActor
enum EOVForecastWidgetEntry {
    static func main() {
        if #available(iOS 18.0, *) {
            EOVSupplementalWidgetBundle.main()
        } else {
            EOVForecastWidgetBundle.main()
        }
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
    var body: some WidgetConfiguration {
        EOVForecastWidget().body.supplementalActivityFamilies([.small, .medium])
    }
}

struct EOVForecastWidget: Widget {
    private let accent = Color(red: 0.94, green: 0.75, blue: 0.42)

    var body: some WidgetConfiguration {
        ActivityConfiguration(for: EOVForecastAttributes.self) { context in
            Group {
                if #available(iOS 18.0, *) {
                    ForecastFamilyContent(context: context)
                } else {
                    ForecastPanel(state: context.state, stale: context.isStale)
                }
            }
                .activityBackgroundTint(Color(red: 0.08, green: 0.11, blue: 0.12))
                .activitySystemActionForegroundColor(accent)
        } dynamicIsland: { context in
            DynamicIsland {
                DynamicIslandExpandedRegion(.leading) { PixelArt(name: "EOVHero", size: 24) }
                DynamicIslandExpandedRegion(.trailing) {
                    Text("Est.").font(.caption2).foregroundStyle(.secondary)
                }
                DynamicIslandExpandedRegion(.bottom) {
                    VStack(alignment: .leading, spacing: 8) {
                        Text(context.state.mapTitle).font(.headline).lineLimit(1)
                        HStack {
                            ForecastNumber(value: context.state.projectedDistance, suffix: " m")
                                .font(.title3.bold())
                            Spacer()
                            Reaps(count: context.state.projectedReaps)
                        }
                        ForecastTime(state: context.state, stale: context.isStale)
                    }.padding(.top, 2).padding(.horizontal, 8).padding(.bottom, 4)
                }
            } compactLeading: {
                PixelArt(name: "EOVHero", size: 24)
            } compactTrailing: {
                Text("~" + compactDistance(context.state.projectedDistance))
                    .font(.caption2.monospacedDigit()).foregroundStyle(accent)
            } minimal: {
                PixelArt(name: "EOVCarl", size: 20)
            }
            .keylineTint(accent)
        }
    }
}

@available(iOS 18.0, *)
private struct ForecastFamilyContent: View {
    @Environment(\.activityFamily) private var family
    let context: ActivityViewContext<EOVForecastAttributes>

    var body: some View {
        if family == .small {
            EOVWatchForecastView(mapTitle: context.state.mapTitle,
                projectedDistance: context.state.projectedDistance,
                projectedReaps: context.state.projectedReaps,
                timeRange: context.state.timeRange, stale: context.isStale,
                ended: context.state.status == "ended")
        } else {
            ForecastPanel(state: context.state, stale: context.isStale)
        }
    }
}

private struct ForecastPanel: View {
    let state: EOVForecastAttributes.ContentState
    let stale: Bool
    var body: some View {
        VStack(alignment: .leading, spacing: 10) {
            HStack(spacing: 10) {
                PixelArt(name: "EOVHero", size: 32)
                VStack(alignment: .leading, spacing: 3) {
                    Text(state.mapTitle).font(.headline).lineLimit(1)
                    HStack(spacing: 4) {
                        Text(distance(state.currentDistance) + " →")
                        ForecastNumber(value: state.projectedDistance, suffix: " m")
                    }.font(.system(.subheadline, design: .rounded).bold())
                        .lineLimit(1).minimumScaleFactor(0.7)
                }
                Spacer(minLength: 4)
                VStack(alignment: .trailing, spacing: 4) {
                    Text("Estimated").font(.caption2).foregroundStyle(.secondary)
                    Reaps(count: state.projectedReaps)
                }
            }
            ForecastTime(state: state, stale: stale)
            HStack(spacing: 18) {
                Metric(art: "EOVTask", count: Double(state.projectedTasks), label: "tasks")
                Metric(art: "EOVKill", count: Double(state.projectedKills), label: "kills")
                Metric(art: "EOVResource", count: state.projectedResources, label: "resources")
                Spacer(minLength: 0)
            }.foregroundStyle(.secondary)
        }.padding(14).foregroundStyle(.white)
    }
}

private struct PixelArt: View {
    let name: String
    let size: CGFloat
    var body: some View {
        Image(name).resizable().interpolation(.none).scaledToFit()
            .frame(width: size, height: size).accessibilityHidden(true)
    }
}

private struct Reaps: View {
    let count: Int
    var body: some View {
        HStack(spacing: 4) {
            // A short pose transition occurs on content updates, never as a perpetual loop.
            PixelArt(name: count.isMultiple(of: 2) ? "EOVCarl" : "EOVCarlSwing", size: 20)
                .id(count).transition(.opacity.combined(with: .scale(scale: 0.85)))
                .animation(.easeOut(duration: 0.4), value: count)
            Text("\(count) " + (count == 1 ? "reap" : "reaps"))
                .font(.caption.monospacedDigit())
        }.accessibilityLabel("\(count) estimated reaps")
    }
}

private struct Metric: View {
    let art: String
    let count: Double
    let label: String
    var body: some View {
        HStack(spacing: 4) {
            PixelArt(name: art, size: 16)
            Text(count.rounded(.down).formatted(.number.precision(.fractionLength(0))))
                .font(.caption.monospacedDigit())
        }.accessibilityLabel("\(count) estimated \(label)")
    }
}

private struct ForecastNumber: View {
    let value: Double
    let suffix: String
    var body: some View {
        if #available(iOS 17.0, *) {
            Text(value.formatted(.number.precision(.fractionLength(0))) + suffix)
                .contentTransition(.numericText())
                .animation(.easeInOut(duration: 0.4), value: value)
        } else {
            Text(value.formatted(.number.precision(.fractionLength(0))) + suffix)
        }
    }
}

private struct ForecastTime: View {
    let state: EOVForecastAttributes.ContentState
    let stale: Bool
    private var timerWidthTemplate: String {
        let duration = state.forecastEnd - state.generatedAt
        return duration >= 3600 ? "0:00:00" : duration >= 600 ? "00:00" : "0:00"
    }

    var body: some View {
        if stale {
            Text("Forecast ended").font(.caption).foregroundStyle(.secondary)
        } else {
            HStack(spacing: 10) {
                ProgressView(timerInterval: state.timeRange, countsDown: false)
                    .labelsHidden().tint(Color(red: 0.94, green: 0.75, blue: 0.42))
                    .frame(maxWidth: .infinity).layoutPriority(1)
                // The hidden template measures the same scalable font without making
                // the date-driven Text reserve an oversized timer layout region.
                Text(timerWidthTemplate).hidden()
                    .overlay(alignment: .trailing) {
                        Text(timerInterval: state.timeRange, countsDown: true)
                            .multilineTextAlignment(.trailing)
                            .lineLimit(1).minimumScaleFactor(0.8)
                    }
                    .font(.caption.monospacedDigit())
                    .fixedSize(horizontal: true, vertical: false)
            }
            .frame(maxWidth: .infinity)
        }
    }
}

private func distance(_ value: Double) -> String {
    value.formatted(.number.precision(.fractionLength(0))) + " m"
}

private func compactDistance(_ value: Double) -> String {
    value >= 1000 ? String(format: "%.1fk", value / 1000) : String(format: "%.0fm", value)
}
