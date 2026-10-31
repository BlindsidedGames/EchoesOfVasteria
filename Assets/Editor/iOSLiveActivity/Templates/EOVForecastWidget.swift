import ActivityKit
import SwiftUI
import WidgetKit

@main
struct EOVForecastWidgetBundle: WidgetBundle {
    var body: some Widget { EOVForecastWidget() }
}

struct EOVForecastWidget: Widget {
    private let accent = Color(red: 0.94, green: 0.75, blue: 0.42)

    var body: some WidgetConfiguration {
        ActivityConfiguration(for: EOVForecastAttributes.self) { context in
            ForecastPanel(state: context.state, stale: context.isStale)
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
                    }.padding(.top, 2)
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
    var body: some View {
        if stale {
            Text("Forecast ended").font(.caption).foregroundStyle(.secondary)
        } else {
            HStack(spacing: 10) {
                ProgressView(timerInterval: state.timeRange, countsDown: false)
                    .labelsHidden().tint(Color(red: 0.94, green: 0.75, blue: 0.42))
                Text(timerInterval: state.timeRange, countsDown: true)
                    .font(.caption.monospacedDigit()).frame(width: 64, alignment: .trailing)
            }
        }
    }
}

private func distance(_ value: Double) -> String {
    value.formatted(.number.precision(.fractionLength(0))) + " m"
}

private func compactDistance(_ value: Double) -> String {
    value >= 1000 ? String(format: "%.1fk", value / 1000) : String(format: "%.0fm", value)
}
