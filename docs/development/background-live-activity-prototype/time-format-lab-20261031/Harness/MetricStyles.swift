import Foundation

public enum LabMetric: String, Codable { case distance, resources, restart }

public struct LabMetricModel: Codable, Hashable {
    public var origin: Date
    public var metric: LabMetric
    public func render(_ date: Date) -> String {
        let seconds = max(0, floor(date.timeIntervalSince(origin)))
        switch metric {
        case .distance: return "\(Int(min(38300, 2000 + 10 * seconds))) / 38300"
        case .resources: return "+\(Int(2 * seconds)) resources"
        case .restart:
            if seconds < 2 { return "\(180 + Int(10 * seconds)) / 200 MOVE" }
            if seconds < 4 { return "200 / 200 RESTART" }
            let phase = Int(seconds - 4) % 22
            return phase < 20 ? "\(phase * 10) / 200 MOVE" : "200 / 200 RESTART"
        }
    }
    public func next(after input: Date) -> Date? {
        if metric == .distance && input.timeIntervalSince(origin) >= 3630 { return nil }
        let current = render(input)
        var tick = max(1, floor(input.timeIntervalSince(origin)) + 1)
        for _ in 0..<100 {
            let date = origin.addingTimeInterval(tick)
            if render(date) != current { return date }
            tick += 1
        }
        return nil
    }
    public func previous(before input: Date) -> Date? {
        let current = render(input)
        var tick = floor(input.timeIntervalSince(origin)) - 0.0001
        for _ in 0..<100 {
            let date = origin.addingTimeInterval(tick)
            if render(date) != current { return date }
            if tick < 0 { return nil }
            tick -= 1
        }
        return nil
    }
}

public struct LabStringStyle: DiscreteFormatStyle {
    public typealias FormatInput = Date
    public typealias FormatOutput = String
    public var model: LabMetricModel
    public func format(_ value: Date) -> String { model.render(value) }
    public func discreteInput(after input: Date) -> Date? { model.next(after: input) }
    public func discreteInput(before input: Date) -> Date? { model.previous(before: input) }
}

public struct LabAttributedStyle: DiscreteFormatStyle {
    public typealias FormatInput = Date
    public typealias FormatOutput = AttributedString
    public var model: LabMetricModel
    public func format(_ value: Date) -> AttributedString { AttributedString(model.render(value)) }
    public func discreteInput(after input: Date) -> Date? { model.next(after: input) }
    public func discreteInput(before input: Date) -> Date? { model.previous(before: input) }
}
