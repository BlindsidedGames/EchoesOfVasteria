import SwiftUI
import ActivityKit
import WidgetKit
import AppIntents

struct RefreshLabView: View {
    var context: ActivityViewContext<RefreshLabAttributes>
    var s: RefreshLabAttributes.ContentState {context.state}
    var body: some View {
        VStack(alignment:.leading,spacing:4) {
            HStack {Text("REFRESH LAB · SYNTHETIC").font(.caption.bold()).foregroundStyle(.orange);Spacer();Button(intent:RefreshSnapshotIntent(run:context.attributes.run)) {Label("Refresh",systemImage:"arrow.clockwise")}.buttonStyle(.borderedProminent)}
            Text("Distance \(s.distance) / 200 · \(s.segment)").font(.headline)
            Text("Resources +\(s.resources) · Reaps \(s.reaps)")
            Text("Refresh #\(s.refreshCount) · sampled \(s.sampledAt.formatted(.dateTime.hour().minute().second()))").font(.caption)
            Text("Executor: \(s.executor)").font(.system(size:10))
            HStack {Text("Elapsed since origin");Text(context.attributes.origin,style:.timer).monospacedDigit()}.font(.caption)
        }.padding(10).activityBackgroundTint(.black.opacity(0.9)).activitySystemActionForegroundColor(.white)
    }
}
@main struct TimeFormatWidget: Widget {
    var body: some WidgetConfiguration {
        ActivityConfiguration(for: RefreshLabAttributes.self) {context in RefreshLabView(context:context)} dynamicIsland: {context in
            DynamicIsland {DynamicIslandExpandedRegion(.center) {RefreshLabView(context:context)}} compactLeading:{Text("LAB")} compactTrailing:{Text("\(context.state.distance)/200")} minimal:{Text("R")}
        }
    }
}
