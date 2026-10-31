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
                            Hero(size: 37, animate: lab.canAnimate)
                            if focus == 0 { DistanceLine(run: run) }
                            else {
                                Text(focus == 1 ? number(run.resources) : duration(run.seconds))
                                    .font(.system(size: 26, weight: .bold, design: .rounded)).monospacedDigit()
                                Text(focus == 1 ? (run.estimated ? "gained" : "resources") : "run time").font(.system(size: 10)).foregroundStyle(Palette.muted)
                            }
                        }
                    }.frame(width: 116, height: 116)
                }.buttonStyle(.plain).accessibilityLabel("Expedition dial. Tap to change the highlighted metric")
                HStack(spacing: 3) {
                    Pixel(name: "Heart", size: 11)
                    Text(number(run.health * 100) + "%").font(.system(size: 10)).foregroundStyle(Palette.muted)
                    if !run.reaping { Text("No distance reap").font(.system(size: 9)).foregroundStyle(Palette.muted) }
                }
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
            VStack(alignment: .leading, spacing: 7) {
                DistanceLine(run: run)
                GeometryReader { geo in
                    ZStack(alignment: .bottomLeading) {
                        HStack(spacing: 0) { ForEach(0..<8) { _ in Image("TrailGrass").resizable().interpolation(.none).frame(width: 24, height: 24) } }
                            .frame(height: 42, alignment: .bottom).clipped()
                        HStack(spacing: 0) { ForEach(0..<8) { _ in Image("TrailWater").resizable().interpolation(.none).frame(width: 24, height: 24) } }
                            .frame(height: 16).offset(y: 16).clipped()
                        Hero(size: 44, running: true, animate: lab.canAnimate)
                            .offset(x: max(0, (geo.size.width - 64) * (run.fraction ?? 0.4)), y: -9)
                        if run.reaping { CarlArrival(size: 32).offset(x: geo.size.width - 34, y: -12) }
                    }.frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .bottomLeading)
                }.frame(height: 60).clipped()
                if let f = run.fraction {
                    GeometryReader { geo in
                        Capsule().fill(Palette.panel)
                        Capsule().fill(Palette.gold).frame(width: max(2, geo.size.width * f))
                    }.frame(height: 4)
                    Text(metres(max(0, run.cap - run.distance)) + " to Carl")
                        .font(.system(size: 11)).foregroundStyle(Palette.muted)
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
        Screen(title: "Vanguard") {
            VStack(spacing: 5) {
                HStack(spacing: 4) {
                    Hero(size: 55, animate: lab.canAnimate)
                    VStack(alignment: .leading, spacing: 1) {
                        HStack(alignment: .firstTextBaseline, spacing: 2) {
                            Text(number(run.health * 100)).font(.system(size: 38, weight: .bold, design: .rounded))
                            Text("%").font(.caption2).foregroundStyle(Palette.muted)
                        }
                        Text("Last phone health").font(.system(size: 10)).foregroundStyle(Palette.muted)
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
        Screen(title: "Harvest") {
            VStack(spacing: 5) {
                HStack(spacing: 5) {
                    Pixel(name: "EOVChest", size: 42)
                    VStack(alignment: .leading, spacing: 0) {
                        Text(averages ? rate(run?.resources ?? 0, run?.durationSeconds ?? 0) : number(run?.resources))
                            .font(.system(size: 36, weight: .bold, design: .rounded)).monospacedDigit().lineLimit(1).minimumScaleFactor(0.6)
                        Text(averages ? "/min · Run average" : "run resources").font(.system(size: 10)).foregroundStyle(Palette.muted)
                    }
                }
                HStack(spacing: 5) {
                    ValueTile(art: "EOVTask", value: averages ? rate(Double(run?.tasks ?? 0), run?.durationSeconds ?? 0) : number(run.map { Double($0.tasks) }), label: averages ? "Tasks per minute, run average" : "Tasks")
                    ValueTile(art: "EOVKill", value: averages ? rate(Double(run?.kills ?? 0), run?.durationSeconds ?? 0) : number(run.map { Double($0.kills) }), label: averages ? "Kills per minute, run average" : "Kills")
                }
                Button { averages.toggle() } label: {
                    HStack { Image(systemName: averages ? "number" : "speedometer"); Text(averages ? "Totals" : "Run averages") }.font(.caption2)
                        .frame(maxWidth: .infinity, minHeight: 44)
                }.buttonStyle(.plain).foregroundStyle(Palette.gold)
            }
        }
    }
}

struct SinceAway: View {
    @EnvironmentObject var lab: LabStore
    var body: some View {
        let run = lab.display
        Screen(title: "Since you left") {
            VStack(spacing: 5) {
                HStack(alignment: .lastTextBaseline) {
                    Text(lab.elapsed).font(.system(size: 29, weight: .bold, design: .rounded)).monospacedDigit()
                    Spacer(minLength: 0)
                    if (run.reaps ?? 0) > 0 { HStack(spacing: 2) { Pixel(name: "EOVCarl", size: 18); Text("New run").font(.system(size: 10)) }.foregroundStyle(Palette.gold) }
                }
                HStack(spacing: 5) {
                    ValueTile(art: "EOVTask", value: number(run.tasks), label: "Task gains since departure")
                    ValueTile(art: "EOVKill", value: number(run.kills), label: "Kill gains since departure")
                }
                HStack(spacing: 5) {
                    ValueTile(art: "EOVChest", value: "+" + number(run.resources), label: "Resource gains since departure")
                    ValueTile(art: "EOVCarl", value: run.reaps.map(String.init) ?? "—", label: "Reaps since departure")
                }
                DistanceLine(run: run)
            }
        }
    }
}

struct RunGoal: View {
    @EnvironmentObject var lab: LabStore
    @State private var resourceGoal = false
    @State private var target = 150.0
    @State private var editing = false
    var body: some View {
        let current = resourceGoal ? (lab.snapshot?.run?.resources ?? 0) : (lab.snapshot?.run?.distance ?? 0)
        Screen(title: "Watch goal") {
            VStack(spacing: 3) {
                Button { editing.toggle() } label: {
                    ZStack {
                        Ring(fraction: min(1, current / max(1, target)), color: Palette.teal, width: 8)
                        VStack(spacing: 2) {
                            Pixel(name: resourceGoal ? "EOVChest" : "EOVHero", size: 23)
                            Text(number(max(0, target - current))).font(.system(size: 31, weight: .bold, design: .rounded))
                            Text(editing ? "Crown to edit" : resourceGoal ? "resources left" : "metres left").font(.system(size: 10)).foregroundStyle(Palette.muted)
                        }
                    }.frame(width: 112, height: 112)
                }.buttonStyle(.plain).focusable(editing)
                    .digitalCrownRotation($target, from: 25, through: 1000, by: 25, sensitivity: .low, isContinuous: false, isHapticFeedbackEnabled: true)
                    .accessibilityLabel("Watch-only goal. Tap and turn Crown to edit")
                HStack(spacing: 0) {
                    Button { resourceGoal.toggle(); target = resourceGoal ? 100 : 150 } label: { Pixel(name: resourceGoal ? "EOVChest" : "EOVHero", size: 20).frame(width: 44, height: 44) }
                        .accessibilityLabel("Change goal metric")
                    Button { target += 25; if target > 1000 { target = 25 } } label: { Text("Goal " + number(target)).font(.caption2).frame(maxWidth: .infinity, minHeight: 44) }
                        .accessibilityLabel("Increase watch goal by25")
                }.buttonStyle(.plain).foregroundStyle(Palette.gold)
                Text("This Watch session only").font(.system(size: 9)).foregroundStyle(Palette.muted)
            }
        }
    }
}

struct SkillBook: View {
    @EnvironmentObject var lab: LabStore
    @State private var selected: String?
    var body: some View {
        Screen(title: "Skillbook") {
            ScrollView {
                if let skills = lab.snapshot?.skills, !skills.isEmpty {
                    LazyVGrid(columns: [GridItem(.flexible()), GridItem(.flexible())], spacing: 5) {
                        ForEach(skills) { skill in
                            Button { selected = selected == skill.id ? nil : skill.id } label: {
                                VStack(spacing: 2) {
                                    Pixel(name: skillArt(skill.id), size: 27)
                                    Text(String(skill.level)).font(.system(size: 25, weight: .bold, design: .rounded)).monospacedDigit()
                                    Text(skill.name).font(.system(size: 10)).lineLimit(1).minimumScaleFactor(0.8)
                                }.frame(maxWidth: .infinity, minHeight: 83)
                                    .background(selected == skill.id ? Palette.gold.opacity(0.2) : Palette.panel, in: RoundedRectangle(cornerRadius: 12))
                                    .overlay(alignment: .topTrailing) { if selected == skill.id { Image(systemName: "pin.fill").font(.system(size: 9)).padding(5).foregroundStyle(Palette.gold) } }
                            }.buttonStyle(.plain).accessibilityLabel(skill.name + ", level " + String(skill.level) + (selected == skill.id ? ", pinned" : ", tap to pin"))
                        }
                    }
                } else { Text("No skills yet").font(.caption) }
            }.scrollIndicators(.hidden)
        }
    }
}

struct RunRibbon: View {
    @EnvironmentObject var lab: LabStore
    @State private var resources = false
    @State private var selection = 9.0
    var body: some View {
        let runs = lab.history.sorted { $0.runNumber < $1.runNumber }
        Screen(title: "Run ribbon") {
            if runs.isEmpty { Text("No recent runs").font(.caption) }
            else {
                let index = min(runs.count - 1, max(0, Int(selection.rounded())))
                let selected = runs[index]
                VStack(spacing: 4) {
                    HStack {
                        Pixel(name: mapArt(selected.mapId), size: 27)
                        VStack(alignment: .leading) {
                            Text(selected.mapTitle).font(.headline)
                            Text((selected.estimated ? "Est. · " : "") + selected.result.capitalized).font(.system(size: 10)).foregroundStyle(Palette.muted)
                        }
                        Spacer(minLength: 0)
                        Text(number(resources ? selected.resources : selected.distance)).font(.system(size: 26, weight: .bold, design: .rounded)).monospacedDigit()
                    }
                    BarRibbon(runs: runs, resources: resources, selection: $selection).frame(height: 70)
                        .focusable().digitalCrownRotation($selection, from: 0, through: Double(max(1, runs.count - 1)), by: 1, sensitivity: .medium, isContinuous: false, isHapticFeedbackEnabled: true)
                    HStack(spacing: 1) {
                        Button { resources.toggle() } label: { HStack(spacing: 3) { Pixel(name: resources ? "EOVChest" : "EOVHero", size: 16); Text(resources ? "Resources" : "Distance").font(.system(size: 10)) }.frame(maxWidth: .infinity, minHeight: 44) }
                            .accessibilityLabel("Change chart metric")
                        NavigationLink { RunDetail(run: selected) } label: { Image(systemName: "chevron.right").frame(width: 44, height: 44) }.accessibilityLabel("Open selected run receipt")
                    }.buttonStyle(.plain).foregroundStyle(Palette.gold)
                    Text("Last " + String(runs.count) + " · ~ estimated").font(.system(size: 9)).foregroundStyle(Palette.muted)
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
            if runs[index].estimated { Text("~").font(.system(size: 9)).foregroundStyle(Palette.muted) }
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
                VStack(spacing: 5) {
                    comparisonRow(first, tone: Palette.gold)
                    comparisonRow(second, tone: Palette.teal)
                    let delta = first.durationSeconds > 0 && second.durationSeconds > 0 ? first.resources / first.durationSeconds * 60 - second.resources / second.durationSeconds * 60 : nil
                    HStack(spacing: 3) {
                        Pixel(name: "EOVChest", size: 17)
                        Text(delta.map { ($0 >= 0 ? "+" : "−") + number(abs($0)) + "/min" } ?? "—")
                            .font(.system(size: 22, weight: .bold, design: .rounded)).monospacedDigit()
                        Spacer(minLength: 0)
                        Button { selected = (selected + 1) % runs.count } label: { Image(systemName: "arrow.left.arrow.right").frame(width: 44, height: 44) }
                            .buttonStyle(.plain).accessibilityLabel("Choose the next pair of recent runs")
                    }.foregroundStyle(Palette.gold)
                    Text(first.mapId == second.mapId ? "Same map · run averages" : "Different maps · run averages").font(.system(size: 9)).foregroundStyle(Palette.muted)
                }
            }
        }
    }
    private func comparisonRow(_ run: EOVWatchSnapshot.RecentRun, tone: Color) -> some View {
        NavigationLink { RunDetail(run: run) } label: {
            HStack(spacing: 6) {
                Pixel(name: mapArt(run.mapId), size: 27)
                VStack(alignment: .leading, spacing: 2) {
                    Text(run.mapTitle + " #" + String(run.runNumber)).font(.system(size: 12, weight: .semibold))
                    Text((run.estimated ? "~ " : "") + run.result.capitalized + " · " + duration(run.durationSeconds)).font(.system(size: 10)).foregroundStyle(Palette.muted)
                    HStack { Text(metres(run.distance)); Spacer(); Text(rate(run.resources, run.durationSeconds) + "/min") }.font(.system(size: 11)).monospacedDigit()
                }
            }.padding(7).frame(minHeight: 61).background(tone.opacity(0.08), in: RoundedRectangle(cornerRadius: 11))
        }.buttonStyle(.plain)
    }
}

struct MapDossier: View {
    @EnvironmentObject var lab: LabStore
    @State private var selection = 0.0
    var body: some View {
        let maps = Array(Set(lab.history.map(\.mapId))).sorted()
        Screen(title: "Map dossier") {
            if maps.isEmpty { Text("No recent map history").font(.caption) }
            else {
                let id = maps[min(maps.count - 1, max(0, Int(selection.rounded())))]
                let runs = lab.history.filter { $0.mapId == id }
                let latest = runs.first!
                let best = runs.filter { !$0.estimated }.map(\.distance).max()
                let totalTime = runs.reduce(0) { $0 + $1.durationSeconds }
                VStack(spacing: 6) {
                    Button { selection = Double((Int(selection) + 1) % maps.count) } label: {
                        HStack(spacing: 7) {
                            Pixel(name: mapArt(id), size: 44)
                            VStack(alignment: .leading, spacing: 2) {
                                Text(latest.mapTitle).font(.system(size: 22, weight: .bold, design: .rounded))
                                Text(String(runs.count) + " of last " + String(lab.history.count)).font(.system(size: 10)).foregroundStyle(Palette.muted)
                            }
                            Spacer(minLength: 0)
                        }.frame(minHeight: 49)
                    }.buttonStyle(.plain).focusable().digitalCrownRotation($selection, from: 0, through: Double(max(1, maps.count - 1)), by: 1, sensitivity: .low, isContinuous: false, isHapticFeedbackEnabled: true)
                        .accessibilityLabel("Selected map " + latest.mapTitle + ". Turn Crown or tap to change map")
                    HStack(alignment: .firstTextBaseline) {
                        Text(number(latest.distance)).font(.system(size: 36, weight: .bold, design: .rounded)).monospacedDigit()
                        Text("m latest" + (latest.estimated ? " ~" : "")).font(.system(size: 10)).foregroundStyle(Palette.muted)
                    }
                    HStack { Text("Recent best"); Spacer(); Text(best.map { metres($0) } ?? "—") }.font(.system(size: 11)).foregroundStyle(Palette.muted)
                    HStack {
                        Metric(art: "EOVChest", value: rate(runs.reduce(0) { $0 + $1.resources }, totalTime) + "/min", label: "Recent run average resources per minute")
                        NavigationLink { RunDetail(run: latest) } label: { Image(systemName: "chevron.right").frame(width: 44, height: 44) }.accessibilityLabel("Open latest run receipt")
                    }.foregroundStyle(Palette.gold)
                }
            }
        }
    }
}
