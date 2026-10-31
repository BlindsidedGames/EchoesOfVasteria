import Foundation
import ActivityKit

struct ClockLabAttributes: ActivityAttributes {
    struct ContentState: Codable, Hashable {
        var origin: Date
    }
    var variant: String
    var run: String
}
