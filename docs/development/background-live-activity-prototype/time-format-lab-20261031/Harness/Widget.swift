import SwiftUI
import ActivityKit
import WidgetKit

struct LabActivityView: View {
    var context: ActivityViewContext<ClockLabAttributes>
    @Environment(\.isLuminanceReduced) private var reduced
    var origin: Date { context.state.origin }
    var variant: String { context.attributes.variant }
    var body: some View {
        VStack(alignment: .leading, spacing: 3) {
            Text("CLOCK LAB · \(variant.uppercased())").font(.caption.bold()).foregroundStyle(.orange)
            if variant == "control" {
                HStack { Text("Native elapsed"); Text(origin, style: .timer).monospacedDigit() }
                Text("Native date progress · virtual start").font(.caption2)
                ProgressView(timerInterval: origin.addingTimeInterval(-200)...origin.addingTimeInterval(3630), countsDown: false) { Text("2000 → 38300 @ 10/s") } currentValueLabel: { EmptyView() }
                ProgressView(timerInterval: origin.addingTimeInterval(-2)...origin.addingTimeInterval(8), countsDown: false) { Text("20 → 100 @ 10/s (8 sec to cap)") } currentValueLabel: { EmptyView() }
                ProgressView(timerInterval: origin.addingTimeInterval(-18)...origin.addingTimeInterval(2), countsDown: false) { Text("180 → 200; no automatic restart") } currentValueLabel: { EmptyView() }
            } else if variant == "system" {
                Text("System TimeDataSource stopwatch")
                Text(TimeDataSource<Date>.currentDate, format: .stopwatch(startingAt: origin)).monospacedDigit()
                Text(TimeDataSource<Date>.currentDate, format: .dateTime.hour().minute().second()).monospacedDigit()
            } else {
                Text("Elapsed distance/cap (10/s)").font(.caption2)
                metric(.distance)
                metric(.resources)
                metric(.restart)
                Text("Restart at +2s, moving again +4s").font(.caption2)
            }
            Text("Reduced luminance: \(reduced ? "yes" : "no")").font(.caption2).foregroundStyle(.secondary)
        }.padding(8).font(.system(size: 12)).activityBackgroundTint(.black.opacity(0.9)).activitySystemActionForegroundColor(.white)
    }
    @ViewBuilder func metric(_ metric: LabMetric) -> some View {
        if variant == "attributed" {
            Text(TimeDataSource<Date>.currentDate, format: LabAttributedStyle(model: .init(origin: origin, metric: metric))).monospacedDigit()
        } else {
            Text(TimeDataSource<Date>.currentDate, format: LabStringStyle(model: .init(origin: origin, metric: metric))).monospacedDigit()
        }
    }
}

@main struct TimeFormatWidget: Widget {
    var body: some WidgetConfiguration {
        ActivityConfiguration(for: ClockLabAttributes.self) { context in LabActivityView(context: context) }
        dynamicIsland: { context in
            DynamicIsland {
                DynamicIslandExpandedRegion(.center) { LabActivityView(context: context) }
            } compactLeading: { Text(context.attributes.variant == "control" ? "CTL" : "LAB") }
              compactTrailing: {
                if context.attributes.variant == "control" { Text(context.state.origin, style: .timer).monospacedDigit() }
                else if context.attributes.variant == "attributed" { Text(TimeDataSource<Date>.currentDate, format: LabAttributedStyle(model: .init(origin: context.state.origin, metric: .distance))) }
                else if context.attributes.variant == "string" { Text(TimeDataSource<Date>.currentDate, format: LabStringStyle(model: .init(origin: context.state.origin, metric: .distance))) }
                else { Text(TimeDataSource<Date>.currentDate, format: .stopwatch(startingAt: context.state.origin)) }
            } minimal: { Text("LAB") }
        }
    }
}
