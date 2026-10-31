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
                        HStack(spacing: 0) { ForEach(0..<8) { _ in Image("TrailWater").resizable().interpolation(.none).frame(width: 24, height: 24) } }
                            .frame(width: geo.size.width, height: 24, alignment: .leading).clipped().offset(y: 28)
                        Pixel(name: "TrailReeds", size: 21).offset(x: 12, y: 5)
                        Pixel(name: "TrailReeds", size: 17).offset(x: geo.size.width * 0.65, y: 15)
                        Hero(size: 34, running: true, animate: lab.canAnimate)
                            .offset(x: max(0, (geo.size.width - 60) * (run.fraction ?? 0.4)), y: 0)
                        if run.reaping { CarlArrival(size: 26).offset(x: geo.size.width - 32, y: 5) }
                    }
                }.frame(height: 46).clipped()
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
        let run = lab.display
        Screen(title: "Health") {
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

struct GoalArc: View {
    let fraction: Double
    var body: some View {
        ZStack {
            ForEach(0..<12) { index in
                Circle().trim(from: Double(index) / 16, to: Double(index + 1) / 16 - 0.012)
                    .stroke(Double(index) / 12 < fraction ? Palette.teal : Palette.teal.opacity(0.15), style: StrokeStyle(lineWidth: 6, lineCap: .round))
                    .rotationEffect(.degrees(135))
            }
        }
    }
}
struct RunGoal: View {
    @EnvironmentObject var lab: LabStore
    @State private var resourceGoal = true
    @State private var target = 100.0
    @State private var editing = false
    @State private var choosing = false
    var body: some View {
        let current = resourceGoal ? lab.display.resources : lab.display.distance
        Screen(title: "Watch goal") {
            VStack(spacing: 3) {
                Button { editing.toggle() } label: {
                    ZStack {
                        GoalArc(fraction: min(1, current / max(1, target)))
                        VStack(spacing: 2) {
                            Group { if resourceGoal { Pixel(name: "EOVChest", size: 23) } else { Image(systemName: "flag.checkered").foregroundStyle(Palette.gold).frame(height: 23) } }
                            Text(number(ceil(max(0, target - current)))).labFont(31, weight: .bold, design: .rounded)
                            Text(editing ? "Crown to edit" : resourceGoal ? (lab.display.estimated ? "gain to goal" : "resources left") : "metres left").labFont(10).foregroundStyle(Palette.muted)
                        }
                    }.frame(width: 88, height: 88)
                }.buttonStyle(.plain).focusable(editing)
                    .digitalCrownRotation($target, from: 25, through: 1000, by: 25, sensitivity: .low, isContinuous: false, isHapticFeedbackEnabled: true)
                    .accessibilityLabel("Watch-only goal. Tap and turn Crown to edit")
                HStack(spacing: 0) {
                    Button { resourceGoal.toggle(); target = resourceGoal ? 100 : 150 } label: { Group { if resourceGoal { Pixel(name: "EOVChest", size: 20) } else { Image(systemName: "ruler").foregroundStyle(Palette.gold) } }.frame(width: 44, height: 44) }
                        .accessibilityLabel("Change goal metric")
                    Button { choosing = true } label: { Text("Edit " + number(target)).font(.caption2).frame(maxWidth: .infinity, minHeight: 44) }
                        .accessibilityLabel("Choose watch goal target")
                }.buttonStyle(.plain).foregroundStyle(Palette.gold)
            }
        }.sheet(isPresented: $choosing) { GoalEditor(target: $target, resourceGoal: $resourceGoal) }
    }
}
struct GoalEditor: View {
    @Binding var target: Double
    @Binding var resourceGoal: Bool
    @Environment(\.dismiss) var dismiss
    var body: some View {
        ScrollView {
            VStack(spacing: 4) {
                Text("Choose goal").font(.headline)
                Button(resourceGoal ? "Resources" : "Distance") { resourceGoal.toggle() }.frame(minHeight: 44)
                Text(number(target) + (resourceGoal ? " resources" : " metres")).font(.title3).focusable()
                    .digitalCrownRotation($target, from: 25, through: 1000, by: 25, sensitivity: .low)
                HStack {
                    Button { target = max(25, target - 25) } label: { Image(systemName: "minus").frame(width: 60, height: 44) }
                    Button { target = min(1000, target + 25) } label: { Image(systemName: "plus").frame(width: 60, height: 44) }
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
                            Text("\(selected.estimated ? "~ " : "")\(selected.result.capitalized) · \(runs.count) runs").labFont(10).foregroundStyle(Palette.muted)
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
                VStack(spacing: 1) {
                    let delta = first.durationSeconds > 0 && second.durationSeconds > 0 ? (first.resources / first.durationSeconds * 60).rounded() - (second.resources / second.durationSeconds * 60).rounded() : nil
                    HStack(spacing: 3) {
                        Pixel(name: "EOVChest", size: 17)
                        Text(delta.map { (first.estimated || second.estimated ? "~ " : "") + ($0 >= 0 ? "+" : "−") + number(abs($0)) + "/min" } ?? "—")
                            .labFont(20, weight: .bold, design: .rounded).monospacedDigit()
                        Spacer(minLength: 0)
                        Button { selected = (selected + 1) % runs.count } label: { Image(systemName: "arrow.left.arrow.right").frame(width: 44, height: 44) }
                            .buttonStyle(.plain).accessibilityLabel("Choose the next pair of recent runs")
                    }.foregroundStyle(Palette.gold)
                    comparisonRow(first, tone: Palette.gold)
                    comparisonRow(second, tone: Palette.teal)
                }
            }
        }
    }
    private func comparisonRow(_ run: EOVWatchSnapshot.RecentRun, tone: Color) -> some View {
        let displayedRate = run.durationSeconds > 0 ? number((run.resources / run.durationSeconds * 60).rounded()) : "—"
        return NavigationLink { RunDetail(run: run) } label: {
            HStack(spacing: 5) {
                Pixel(name: mapArt(run.mapId), size: 22)
                VStack(alignment: .leading, spacing: 2) {
                    Text("\(run.mapTitle) #\(run.runNumber) · \(run.estimated ? "~ " : "")\(run.result.capitalized)").labFont(10, weight: .semibold).lineLimit(1)
                    HStack(spacing: 3) {
                        Text(duration(run.durationSeconds)); Text("·"); Text(metres(run.distance)); Spacer(minLength: 0); Text(displayedRate + "/min")
                    }.labFont(10).monospacedDigit()
                }
            }.padding(.horizontal, 5).frame(minHeight: 44).background(tone.opacity(0.08), in: RoundedRectangle(cornerRadius: 10))
        }.buttonStyle(.plain)
    }
}

struct MapDossier: View {
    @EnvironmentObject var lab: LabStore
    @State private var selection = 0.0
    private let maps = ["River", "Woods", "Mines", "Farmlands", "Haloween"]
    private func title(_ id: String) -> String { id == "Haloween" ? "Hollow" : id }
    var body: some View {
        let id = maps[min(maps.count - 1, max(0, Int(selection.rounded())))]
        let runs = lab.history.filter { $0.mapId == id }
        let latest = runs.first
        let best = runs.filter { !$0.estimated }.map(\.distance).max()
        let totalTime = runs.reduce(0) { $0 + $1.durationSeconds }
        Screen(title: "Maps") {
            VStack(spacing: 5) {
                Button { selection = Double((Int(selection) + 1) % maps.count) } label: {
                    HStack(spacing: 7) {
                        Pixel(name: mapArt(id), size: 27)
                        VStack(alignment: .leading, spacing: 1) {
                            Text(title(id)).labFont(17, weight: .bold, design: .rounded)
                            Text(String(runs.count) + " of last " + String(lab.history.count)).labFont(10).foregroundStyle(Palette.muted)
                        }
                        Spacer(minLength: 0)
                        if let latest {
                            VStack(alignment: .trailing, spacing: 0) {
                                Text(number(latest.distance)).labFont(25, weight: .bold, design: .rounded).monospacedDigit()
                                Text("m latest" + (latest.estimated ? " ~" : "")).labFont(9).foregroundStyle(Palette.muted)
                            }
                        }
                        Image(systemName: "chevron.up.chevron.down").labFont(10).foregroundStyle(Palette.gold)
                    }.frame(minHeight: 44)
                }.buttonStyle(.plain).focusable()
                    .digitalCrownRotation($selection, from: 0, through: Double(maps.count - 1), by: 1, sensitivity: .low, isContinuous: false, isHapticFeedbackEnabled: true)
                    .accessibilityLabel("Selected map " + title(id) + ". Turn Crown or tap to change map")
                if let latest {
                    HStack { Text("Recorded best"); Spacer(); Text(best.map { number($0) + " m" } ?? "—") }.labFont(10).foregroundStyle(Palette.muted)
                    HStack {
                        VStack(alignment: .leading, spacing: 1) {
                            Metric(art: "EOVChest", value: (runs.contains(where: { $0.estimated }) ? "~ " : "") + rate(runs.reduce(0) { $0 + $1.resources }, totalTime) + "/min", label: "Recent resource rate, including estimated records")
                            Text(runs.contains(where: { $0.estimated }) ? "Includes estimates" : "Recent run average").labFont(9).foregroundStyle(Palette.muted)
                        }
                        NavigationLink { RunDetail(run: latest) } label: { Image(systemName: "chevron.right").frame(width: 44, height: 44) }.buttonStyle(.plain).accessibilityLabel("Open latest run receipt")
                    }.foregroundStyle(Palette.gold)
                } else {
                    Text("No recent records").labFont(17, weight: .semibold).padding(.top, 12)
                    Text("This map has no runs in the retained ten.").labFont(12).foregroundStyle(Palette.muted).multilineTextAlignment(.center)
                }
            }
        }
    }
}
