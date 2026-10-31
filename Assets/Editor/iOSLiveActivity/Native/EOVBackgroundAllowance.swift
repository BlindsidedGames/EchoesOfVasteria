import Foundation

// Display metadata from the managed policy, independent of a shorter forecast.
// Optional invalid/legacy values do not invalidate otherwise useful progress.
enum EOVBackgroundAllowance {
    static func validated(_ seconds: Double?) -> Double? {
        guard let seconds, seconds.isFinite, seconds > 0 else { return nil }
        return seconds
    }
    static func label(_ seconds: Double?) -> String? {
        guard let seconds = validated(seconds) else { return nil }
        let (scale, unit): (Double, String) = seconds >= 3600 ? (3600, "h") : seconds >= 60 ? (60, "m") : (1, "s")
        return (seconds / scale).formatted(.number.precision(.fractionLength(0...1))) + unit + " max"
    }
}
