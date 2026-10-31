import SwiftUI

struct ExpeditionDial: View {
    @EnvironmentObject var lab: LabStore
    @State private var focus = 0
    var body: some View {
        let run = lab.display
        Screen(title: run.map) {
            VStack(spacing: 6) {
                Button { focus = (focus + 1) % 3 } label: {
                    ZStack {
                        Ring(fraction: run.fraction)
                        VStack(spacing: 0) {
                            Hero(size: 44, animate: lab.canAnimate)
                            if focus == 0 { DistanceLine(run: run) }
                            else {
                                Text(focus == 1 ? number(run.resources) : duration(run.seconds))
                                    .labFont(26, weight: .bold, design: .rounded).monospacedDigit()
                                Text(focus == 1 ? (run.estimated ? "gained" : "resources") : "run time").labFont(10).foregroundStyle(Palette.muted)
                            }
                            HStack(spacing: 2) {
                                Pixel(name: "Heart", size: 9)
                                Text((run.estimated ? "Last " : "") + number((run.health * 100).rounded()) + "%").labFont(9).foregroundStyle(Palette.muted)
                            }
                        }
                    }.frame(width: 100, height: 100)
                }.buttonStyle(.plain).accessibilityLabel("Expedition dial. Tap to change the highlighted metric")
                Quartet(run: run)
            }
        }
    }
}

struct LivingTrail: View {
    @EnvironmentObject var lab: LabStore
    var body: some View {
        let run = lab.display
        Screen(title: run.map) {
            VStack(alignment: .leading, spacing: 3) {
                DistanceLine(run: run)
                GeometryReader { geo in
                    ZStack(alignment: .topLeading) {
                        HStack(spacing: 0) { ForEach(0..<8) { _ in Image("TrailGrass").resizable().interpolation(.none).frame(width: 24, height: 24) } }
                            .frame(width: geo.size.width, height: 24, alignment: .leading).clipped().offset(y: 17)
                        HStack(spacing: 0) { ForEach(0..<8) { _ in Image("TrailWater").resizable().interpolation(.none).frame(width: 24, height: 36) } }
                            .frame(width: geo.size.width, height: 36, alignment: .leading).clipped().offset(y: 28)
                        Pixel(name: "TrailReeds", size: 21).offset(x: 12, y: 5)
                        Pixel(name: "TrailReeds", size: 17).offset(x: geo.size.width * 0.65, y: 15)
                        TrailHero().position(x: 18 + (geo.size.width - 56) * (run.fraction ?? 0.4), y: 26.75)
                        if run.reaping { CarlArrival().position(x: geo.size.width - 21.125, y: 26.75) }
                    }
                }.frame(height: 54).clipped()
                if let f = run.fraction {
                    GeometryReader { geo in
                        Capsule().fill(Palette.panel)
                        Capsule().fill(Palette.gold).frame(width: max(2, geo.size.width * f))
                    }.frame(height: 4)
                    Text(metres(max(0, run.cap - run.distance)) + " to Carl")
                        .labFont(11).foregroundStyle(Palette.muted)
                } else { Text("Open route").font(.caption2).foregroundStyle(Palette.muted) }
                Spacer(minLength: 0)
                Quartet(run: run)
            }
        }
    }
}

struct CombatWatch: View {
    @EnvironmentObject var lab: LabStore
    var body: some View {
        let run = RunDisplay(lab.snapshot, at: lab.clock, allowEstimate: false)
        Screen(title: lab.runStatus ?? "Health") {
            VStack(spacing: 5) {
                HStack(spacing: 4) {
                    Hero(size: 55, animate: lab.canAnimate)
                    VStack(alignment: .leading, spacing: 1) {
                        HStack(alignment: .firstTextBaseline, spacing: 2) {
                            Text(number((run.health * 100).rounded())).labFont(38, weight: .bold, design: .rounded)
                            Text("%").font(.caption2).foregroundStyle(Palette.muted)
                        }
                        Text("Phone health · " + lab.age + " ago").labFont(10).foregroundStyle(Palette.muted)
                    }
                }
                GeometryReader { geo in
                    Capsule().fill(Palette.red.opacity(0.15))
                    Capsule().fill(run.health > 0.3 ? Palette.teal : Palette.red)
                        .frame(width: max(0, geo.size.width * run.health))
                }.frame(height: 7)
                HStack { Text(run.map); Spacer(); Text(metres(run.distance)) }.font(.caption2).foregroundStyle(Palette.muted)
                Spacer(minLength: 0)
                HStack {
                    Metric(art: "EOVKill", value: number(lab.snapshot?.run.map { Double($0.kills) }), label: "Actual run kills", big: true)
                    Metric(art: "EOVChest", value: number(lab.snapshot?.run?.resources), label: "Actual run resources", big: true)
                }
            }
        }
    }
}

struct HarvestCounter: View {
    @EnvironmentObject var lab: LabStore
    @State private var averages = false
    var body: some View {
        let run = lab.snapshot?.run
        Screen(title: "Resources") {
            VStack(spacing: 5) {
                HStack(spacing: 5) {
                    Pixel(name: "EOVChest", size: 42)
                    VStack(alignment: .leading, spacing: 0) {
                        Text(averages ? rate(run?.resources ?? 0, run?.durationSeconds ?? 0) : number(run?.resources))
                            .labFont(36, weight: .bold, design: .rounded).monospacedDigit().lineLimit(1).minimumScaleFactor(0.6)
                        Text(averages ? "/min · Run average" : "run resources").labFont(10).foregroundStyle(Palette.muted)
                    }
                }
                HStack(spacing: 5) {
                    GainCell(art: "EOVTask", value: averages ? rate(Double(run?.tasks ?? 0), run?.durationSeconds ?? 0) : number(run.map { Double($0.tasks) }), label: averages ? "Tasks per minute, run average" : "Tasks")
                    GainCell(art: "EOVKill", value: averages ? rate(Double(run?.kills ?? 0), run?.durationSeconds ?? 0) : number(run.map { Double($0.kills) }), label: averages ? "Kills per minute, run average" : "Kills")
                }
                Button { averages.toggle() } label: {
                    HStack { Image(systemName: averages ? "number" : "speedometer"); Text(averages ? "Totals" : "Run averages") }.font(.caption2)
                        .frame(maxWidth: .infinity, minHeight: 44)
                }.buttonStyle(.plain).foregroundStyle(Palette.gold)
            }
        }
    }
}

struct GainCell: View {
    let art: String
    let value: String
    let label: String
    var body: some View {
        HStack(spacing: 5) {
            Pixel(name: art, size: 18)
            Text(value).labFont(21, weight: .bold, design: .rounded).monospacedDigit().lineLimit(1)
        }.frame(maxWidth: .infinity, minHeight: 34)
            .background(Palette.panel, in: RoundedRectangle(cornerRadius: 10))
            .accessibilityElement(children: .ignore).accessibilityLabel(label).accessibilityValue(value)
    }
}
struct SinceAway: View {
    @EnvironmentObject var lab: LabStore
    var body: some View {
        let run = lab.display
        Screen(title: "Since you left") {
            if run.estimated {
                VStack(spacing: 3) {
                    HStack(alignment: .lastTextBaseline) {
                        Text(lab.elapsed).labFont(27, weight: .bold, design: .rounded).monospacedDigit()
                        Spacer(minLength: 0)
                        if (run.reaps ?? 0) > 0 { Text(run.phase == 1 ? "Restarting" : "New run").labFont(10).foregroundStyle(Palette.gold) }
                    }
                    HStack(spacing: 5) {
                        GainCell(art: "EOVTask", value: number(run.tasks), label: "Task gains since departure")
                        GainCell(art: "EOVKill", value: number(run.kills), label: "Kill gains since departure")
                    }
                    HStack(spacing: 5) {
                        GainCell(art: "EOVChest", value: "+" + number(run.resources), label: "Resource gains since departure")
                        GainCell(art: "EOVCarl", value: run.reaps.map(String.init) ?? "—", label: "Reaps since departure")
                    }
                    HStack(alignment: .firstTextBaseline, spacing: 3) {
                        Text(number(run.distance)).labFont(20, weight: .bold, design: .rounded)
                        Text(run.reaping ? "/ " + number(run.cap) + " m" : "m").labFont(11).foregroundStyle(Palette.muted)
                    }.monospacedDigit()
                }
            } else {
                VStack(spacing: 10) {
                    Hero(size: 42, animate: false)
                    Text("No away estimate yet").font(.headline)
                    Text("Your current run remains on iPhone.").font(.caption2).foregroundStyle(Palette.muted)
                }
            }
        }
    }
}

struct SteppedGoalTrack: View {
    let fraction: Double
    var body: some View {
        HStack(spacing: 3) {
            ForEach(0..<10) { index in
                GeometryReader { geo in
                    RoundedRectangle(cornerRadius: 2).fill(Palette.teal.opacity(0.15))
                    RoundedRectangle(cornerRadius: 2).fill(Palette.teal)
                        .frame(width: geo.size.width * CGFloat(max(0, min(1, fraction * 10 - Double(index)))))
                }
            }
        }.frame(height: 8).accessibilityLabel("Watch-session goal progress")
    }
}
let watchGoalTargets: [Double] = [100, 500, 1000, 5000, 10000, 50000, 100000, 500000, 1e6, 5e6, 1e7, 5e7, 1e8, 5e8, 1e9, 5e9, 1e10, 5e10, 1e11]
struct RunGoal: View {
    @EnvironmentObject var lab: LabStore
    @State private var resourceGoal = true
    @State private var goalIndex = 0.0
    @State private var initialized = false
    private var target: Double { watchGoalTargets[min(watchGoalTargets.count - 1, max(0, Int(goalIndex.rounded())))] }
    @State private var editing = false
    @State private var choosing = false
    var body: some View {
        let phone = RunDisplay(lab.snapshot, at: lab.clock, allowEstimate: false)
        let current = resourceGoal ? phone.resources : phone.distance
        let remaining = ceil(max(0, target - current))
        Screen(title: "Watch goal") {
            VStack(spacing: 5) {
                Button { editing.toggle() } label: {
                    HStack(spacing: 9) {
                        if resourceGoal { Pixel(name: "EOVChest", size: 34) }
                        else { Image(systemName: "flag.checkered").labFont(27).foregroundStyle(Palette.gold) }
                        VStack(alignment: .leading, spacing: 0) {
                            Text(number(remaining)).labFont(37, weight: .bold, design: .rounded).monospacedDigit()
                            Text(remaining == 0 ? "Goal reached" : editing ? "Crown to set goal" : resourceGoal ? "resources left" : "metres left").labFont(11).foregroundStyle(Palette.muted)
                        }
                        Spacer(minLength: 0)
                    }.frame(minHeight: 44)
                }.buttonStyle(.plain).focusable(editing)
                    .digitalCrownRotation($goalIndex, from: 0, through: Double(watchGoalTargets.count - 1), by: 1, sensitivity: .low, isContinuous: false, isHapticFeedbackEnabled: true)
                    .accessibilityLabel("Watch-session goal remaining. Tap then turn Crown to edit")
                SteppedGoalTrack(fraction: min(1, current / max(1, target)))
                HStack {
                    Text(number(current) + " / " + number(target) + (resourceGoal ? " resources" : " metres")).labFont(12).monospacedDigit()
                    Spacer(minLength: 0)
                }.foregroundStyle(Palette.muted)
                Button { choosing = true } label: {
                    HStack(spacing: 5) { Image(systemName: "pencil"); Text("Edit goal") }.font(.caption2)
                        .frame(maxWidth: .infinity, minHeight: 44)
                        .background(Palette.panel, in: RoundedRectangle(cornerRadius: 10))
                }.buttonStyle(.plain).foregroundStyle(Palette.gold).accessibilityLabel("Choose watch goal metric and target")
            }
        }.sheet(isPresented: $choosing) { GoalEditor(goalIndex: $goalIndex, resourceGoal: $resourceGoal) }
            .onAppear {
                guard !initialized else { return }; initialized = true
                goalIndex = Double(watchGoalTargets.firstIndex(where: { $0 > current }) ?? watchGoalTargets.count - 1)
            }
    }
}
struct GoalEditor: View {
    @EnvironmentObject var lab: LabStore
    @Binding var goalIndex: Double
    @Binding var resourceGoal: Bool
    private var target: Double { watchGoalTargets[min(watchGoalTargets.count - 1, max(0, Int(goalIndex.rounded())))] }
    @Environment(\.dismiss) var dismiss
    var body: some View {
        ScrollView {
            VStack(spacing: 4) {
                Text("Choose goal").font(.headline)
                Button(resourceGoal ? "Resources" : "Distance") {
                    resourceGoal.toggle()
                    let current = resourceGoal ? (lab.snapshot?.run?.resources ?? 0) : (lab.snapshot?.run?.distance ?? 0)
                    goalIndex = Double(watchGoalTargets.firstIndex(where: { $0 > current }) ?? watchGoalTargets.count - 1)
                }.frame(minHeight: 44)
                Text(number(target) + (resourceGoal ? " resources" : " metres")).font(.title3).focusable()
                    .digitalCrownRotation($goalIndex, from: 0, through: Double(watchGoalTargets.count - 1), by: 1, sensitivity: .low)
                HStack {
                    Button { goalIndex = max(0, goalIndex - 1) } label: { Image(systemName: "minus").frame(width: 60, height: 44) }
                    Button { goalIndex = min(Double(watchGoalTargets.count - 1), goalIndex + 1) } label: { Image(systemName: "plus").frame(width: 60, height: 44) }
                }
                Text("This Watch session only").font(.caption2)
                Button("Done") { dismiss() }.frame(minHeight: 44)
            }
        }
    }
}



struct SkillBook: View {
    @EnvironmentObject var lab: LabStore
    @State private var selected: String?
    var body: some View {
        Screen(title: "Skillbook") {
            Group {
                if let skills = lab.snapshot?.skills, !skills.isEmpty {
                    LazyVGrid(columns: [GridItem(.flexible()), GridItem(.flexible())], spacing: 3) {
                        ForEach(skills) { skill in
                            Button { selected = selected == skill.id ? nil : skill.id } label: {
                                VStack(spacing: 1) {
                                    HStack(spacing: 4) {
                                        Pixel(name: skillArt(skill.id), size: 18)
                                        Text(String(skill.level)).labFont(21, weight: .bold, design: .rounded).monospacedDigit()
                                    }
                                    Text(skill.name).labFont(10).lineLimit(1).minimumScaleFactor(0.8)
                                }.frame(maxWidth: .infinity, minHeight: 44)
                                    .background(selected == skill.id ? Palette.gold.opacity(0.2) : Palette.panel, in: RoundedRectangle(cornerRadius: 12))
                                    .overlay(alignment: .topTrailing) { if selected == skill.id { Image(systemName: "pin.fill").labFont(9).padding(5).foregroundStyle(Palette.gold) } }
                            }.buttonStyle(.plain).accessibilityLabel(skill.name + ", level " + String(skill.level) + (selected == skill.id ? ", pinned" : ", tap to pin"))
                        }
                    }
                } else { Text("No skills yet").font(.caption) }
            }
        }
    }
}

struct RunRibbon: View {
    @EnvironmentObject var lab: LabStore
    @State private var resources = false
    @State private var selection = 9.0
    var body: some View {
        let runs = lab.history.sorted { $0.runNumber < $1.runNumber }
        Screen(title: "Recent runs") {
            if runs.isEmpty { Text("No recent runs").font(.caption) }
            else {
                let index = min(runs.count - 1, max(0, Int(selection.rounded())))
                let selected = runs[index]
                VStack(spacing: 4) {
                    HStack {
                        Pixel(name: outcome(selected), size: 22)
                        VStack(alignment: .leading) {
                            Text(selected.mapTitle).font(.headline)
                            Text("#\(selected.runNumber) · \(selected.estimated ? "~ " : "")\(selected.result.capitalized)").labFont(10).foregroundStyle(Palette.muted)
                        }
                        Spacer(minLength: 0)
                        VStack(alignment: .trailing, spacing: 0) {
                            Text(number(resources ? selected.resources : selected.distance)).labFont(26, weight: .bold, design: .rounded).monospacedDigit()
                            Text(resources ? "resources" : "metres").labFont(9).foregroundStyle(Palette.muted)
                        }
                    }
                    BarRibbon(runs: runs, resources: resources, selection: $selection).frame(height: 45)
                        .focusable().digitalCrownRotation($selection, from: 0, through: Double(max(1, runs.count - 1)), by: 1, sensitivity: .medium, isContinuous: false, isHapticFeedbackEnabled: true)
                    HStack(spacing: 1) {
                        Button { resources.toggle() } label: { HStack(spacing: 3) { Pixel(name: resources ? "EOVChest" : "EOVHero", size: 16); Text(resources ? "Resources" : "Distance").labFont(10) }.frame(maxWidth: .infinity, minHeight: 44) }
                            .accessibilityLabel("Change chart metric")
                        NavigationLink { RunDetail(run: selected) } label: { Image(systemName: "chevron.right").frame(width: 44, height: 44) }.accessibilityLabel("Open selected run receipt")
                    }.buttonStyle(.plain).foregroundStyle(Palette.gold)
                }
            }
        }
    }
}
struct BarRibbon: View {
    let runs: [EOVWatchSnapshot.RecentRun]
    let resources: Bool
    @Binding var selection: Double
    private var values: [Double] { runs.map { resources ? $0.resources : $0.distance } }
    private var maximum: Double { max(1.0, values.max() ?? 1.0) }
    private func bar(_ index: Int, height: CGFloat) -> some View {
        let selected = Int(selection.rounded()) == index
        let tone: Color = selected ? Palette.gold : (runs[index].estimated ? Palette.teal.opacity(0.38) : Palette.teal)
        let barHeight = max(CGFloat(2), (height - 13) * CGFloat(values[index] / maximum))
        return VStack(spacing: 2) {
            if runs[index].estimated { Text("~").labFont(9).foregroundStyle(Palette.muted) }
            RoundedRectangle(cornerRadius: 3).fill(tone).frame(height: barHeight)
        }.frame(maxWidth: .infinity)
    }
    var body: some View {
        GeometryReader { geo in
            ZStack(alignment: .bottom) {
                Rectangle().fill(Palette.muted.opacity(0.2)).frame(height: 1)
                HStack(alignment: .bottom, spacing: 4) {
                    ForEach(0..<runs.count, id: \.self) { index in bar(index, height: geo.size.height) }
                }
            }
            .contentShape(Rectangle())
            .gesture(DragGesture(minimumDistance: 0).onChanged { value in
                let fraction = Double(value.location.x / max(CGFloat(1), geo.size.width))
                selection = Double(min(runs.count - 1, max(0, Int(fraction * Double(runs.count)))))
            })
        }.accessibilityLabel("Recent run bar chart, common zero baseline. Turn Crown or touch chart to select")
    }
}

struct RunComparison: View {
    @EnvironmentObject var lab: LabStore
    @State private var selected = 0
    var body: some View {
        let all = lab.history
        let map = lab.snapshot?.run?.mapId
        let same = all.filter { $0.mapId == map }
        let runs = same.count >= 2 ? same : all
        Screen(title: "Compare runs") {
            if runs.count < 2 { Text("Two recent runs needed").font(.caption) }
            else {
                let first = runs[selected % runs.count]
                let second = runs[(selected + 1) % runs.count]
                let maximum = max(rawRate(first), rawRate(second))
                let unit = maximum >= 1e9 ? 1e9 : maximum >= 1e6 ? 1e6 : maximum >= 1e3 ? 1e3 : 1
                VStack(spacing: 1) {
                    let delta = first.durationSeconds > 0 && second.durationSeconds > 0 ? (rawRate(first) / unit).rounded() * unit - (rawRate(second) / unit).rounded() * unit : nil
                    HStack(spacing: 3) {
                        Pixel(name: "EOVChest", size: 17)
                        Text(delta.map { (first.estimated || second.estimated ? "~ " : "") + ($0 >= 0 ? "+" : "−") + number(abs($0)) + "/min" } ?? "—")
                            .labFont(20, weight: .bold, design: .rounded).monospacedDigit()
                        Spacer(minLength: 0)
                        Button { selected = (selected + 1) % runs.count } label: { Image(systemName: "arrow.left.arrow.right").frame(width: 44, height: 44) }
                            .buttonStyle(.plain).accessibilityLabel("Choose the next pair of recent runs")
                    }.foregroundStyle(Palette.gold)
                    comparisonRow(first, tone: Palette.gold, unit: unit, maximum: maximum)
                    comparisonRow(second, tone: Palette.teal, unit: unit, maximum: maximum)
                }
            }
        }
    }
    private func rawRate(_ run: EOVWatchSnapshot.RecentRun) -> Double { run.durationSeconds > 0 ? run.resources / run.durationSeconds * 60 : 0 }
    private func comparisonRow(_ run: EOVWatchSnapshot.RecentRun, tone: Color, unit: Double, maximum: Double) -> some View {
        let displayedRate = run.durationSeconds > 0 ? number((rawRate(run) / unit).rounded() * unit) : "—"
        return NavigationLink { RunDetail(run: run) } label: {
            HStack(spacing: 5) {
                Pixel(name: mapArt(run.mapId), size: 22)
                VStack(alignment: .leading, spacing: 2) {
                    Text("\(run.mapTitle) #\(run.runNumber) · \(run.estimated ? "~ " : "")\(run.result.capitalized)").labFont(10, weight: .semibold).lineLimit(1)
                    HStack(spacing: 3) {
                        Text(duration(run.durationSeconds)); Text("·"); Text(number(run.distance) + " m"); Spacer(minLength: 0); Text(displayedRate + "/min")
                    }.labFont(10).monospacedDigit()
                }
            }.padding(.horizontal, 5).frame(minHeight: 44).background(tone.opacity(0.08), in: RoundedRectangle(cornerRadius: 10))
                .overlay(alignment: .bottomLeading) {
                    GeometryReader { geo in Capsule().fill(tone).frame(width: geo.size.width * CGFloat(min(1, rawRate(run) / max(1, maximum)))) }.frame(height: 2)
                }
        }.buttonStyle(.plain)
    }
}

struct MapDossier: View {
    @Environment(\.dynamicTypeSize) var textSize
    @EnvironmentObject var lab: LabStore
    @State private var selection = 0.0
    @State private var selectedInitialMap = false
    private var maps: [String] {
        Array(Set(lab.history.map(\.mapId) + [lab.snapshot?.run?.mapId].compactMap { $0 })).sorted()
    }
    private func title(_ id: String) -> String {
        if lab.snapshot?.run?.mapId == id { return lab.snapshot?.run?.mapTitle ?? id }
        return lab.history.first(where: { $0.mapId == id })?.mapTitle ?? id
    }
    var body: some View {
        let id = maps.isEmpty ? "" : maps[min(maps.count - 1, max(0, Int(selection.rounded())))]
        let runs = lab.history.filter { $0.mapId == id }
        let latest = runs.first
        let best = runs.filter { !$0.estimated }.map(\.distance).max()
        let totalTime = runs.reduce(0) { $0 + $1.durationSeconds }
        Screen(title: "Maps") {
            if maps.isEmpty { Text("No recent map data").font(.headline) }
            else {
            VStack(spacing: 5) {
                Button { selection = Double((Int(selection) + 1) % maps.count) } label: {
                    HStack(spacing: 7) {
                        Pixel(name: mapArt(id), size: 27)
                        VStack(alignment: .leading, spacing: 1) {
                            Text(title(id)).labFont(17, weight: .bold, design: .rounded)
                            Text(String(runs.count) + " of last " + String(lab.history.count)).labFont(10).foregroundStyle(Palette.muted)
                        }
                        Spacer(minLength: 0)
                        if let latest, !textSize.isAccessibilitySize {
                            VStack(alignment: .trailing, spacing: 0) {
                                Text(number(latest.distance)).labFont(25, weight: .bold, design: .rounded).monospacedDigit()
                                Text("m last run" + (latest.estimated ? " ~" : "")).labFont(9).foregroundStyle(Palette.muted)
                            }
                        }
                        Image(systemName: "chevron.up.chevron.down").labFont(10).foregroundStyle(Palette.gold)
                    }.frame(minHeight: 44)
                }.buttonStyle(.plain).focusable()
                    .digitalCrownRotation($selection, from: 0, through: Double(maps.count - 1), by: 1, sensitivity: .low, isContinuous: false, isHapticFeedbackEnabled: true)
                    .accessibilityLabel("Selected map " + title(id) + ". Turn Crown or tap to change map")
                if let latest {
                    if textSize.isAccessibilitySize {
                        HStack(alignment: .firstTextBaseline) {
                            Text(number(latest.distance)).labFont(32, weight: .bold, design: .rounded).lineLimit(1)
                            Text("m last run" + (latest.estimated ? " ~" : "")).labFont(11).foregroundStyle(Palette.muted)
                        }
                    }
                    HStack { Text("Recent recorded best"); Spacer(); Text(best.map { number($0) + " m" } ?? "—") }.labFont(10).foregroundStyle(Palette.muted)
                    if lab.snapshot?.run?.mapId == id, let current = lab.snapshot?.run {
                        HStack { Text("Current run"); Spacer(); Text(number(current.distance) + " m") }.labFont(10).foregroundStyle(Palette.muted)
                    }
                    HStack {
                        VStack(alignment: .leading, spacing: 1) {
                            Metric(art: "EOVChest", value: (runs.contains(where: { $0.estimated }) ? "~ " : "") + rate(runs.reduce(0) { $0 + $1.resources }, totalTime) + "/min", label: "Recent resource rate, including estimated records")
                            Text(runs.contains(where: { $0.estimated }) ? "Includes estimates" : "Recent run average").labFont(9).foregroundStyle(Palette.muted)
                        }
                        NavigationLink { RunDetail(run: latest) } label: { Image(systemName: "chevron.right").frame(width: 44, height: 44) }.buttonStyle(.plain).accessibilityLabel("Open latest run receipt")
                    }.foregroundStyle(Palette.gold)
                } else {
                    Text("No recent records").labFont(17, weight: .semibold).padding(.top, 12)
                    Text("No retained runs for this map.").labFont(12).foregroundStyle(Palette.muted).multilineTextAlignment(.center)
                }
            }
            }
        }.onAppear {
            guard !selectedInitialMap else { return }
            selectedInitialMap = true
            selection = Double(maps.firstIndex(of: lab.snapshot?.run?.mapId ?? "River") ?? 0)
        }
    }
}
