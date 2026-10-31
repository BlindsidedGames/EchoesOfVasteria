import SwiftUI

// Shared by the iPhone-hosted small Live Activity and native Watch rendering.
// Values come directly from the existing forecast ContentState; no simulation runs here.
struct EOVWatchForecastView: View {
    let mapTitle: String
    let projectedDistance: Double
    let projectedReaps: Int
    let timeRange: ClosedRange<Date>
    let stale: Bool
    let ended: Bool
    @Environment(\.isLuminanceReduced) private var reducedLuminance

    private var accent: Color {
        Color(red: 0.94, green: 0.75, blue: 0.42).opacity(reducedLuminance ? 0.65 : 1)
    }

    var body: some View {
        HStack(spacing: 6) {
            ZStack {
                if stale || ended {
                    Circle().stroke(.secondary.opacity(0.35), lineWidth: 4)
                } else {
                    ProgressView(timerInterval: timeRange, countsDown: false) {
                        EmptyView()
                    } currentValueLabel: {
                        EmptyView()
                    }
                        .progressViewStyle(.circular).labelsHidden().tint(accent)
                }
                Image(projectedReaps.isMultiple(of: 2) ? "EOVCarl" : "EOVCarlSwing")
                    .resizable().interpolation(.none).scaledToFit()
                    .frame(width: 26, height: 26).accessibilityHidden(true)
            }
            .frame(width: 44, height: 44)
            VStack(alignment: .leading, spacing: 2) {
                Text(mapTitle).font(.caption2.bold()).lineLimit(1).minimumScaleFactor(0.8)
                if stale || ended {
                    Text(ended ? "Ended" : "Forecast ended")
                        .font(.caption).foregroundStyle(.secondary).lineLimit(1).minimumScaleFactor(0.7)
                } else {
                    Text(timerInterval: timeRange, countsDown: true)
                        .font(.headline.monospacedDigit()).lineLimit(1).minimumScaleFactor(0.7)
                }
                Text("Est. " + projectedDistance.formatted(.number.precision(.fractionLength(0))) +
                    " m · \(projectedReaps) " + (projectedReaps == 1 ? "reap" : "reaps"))
                    .font(.caption2).foregroundStyle(.secondary).lineLimit(1).minimumScaleFactor(0.7)
            }
            .frame(maxWidth: .infinity, alignment: .leading)
        }
        .padding(6)
    }
}
