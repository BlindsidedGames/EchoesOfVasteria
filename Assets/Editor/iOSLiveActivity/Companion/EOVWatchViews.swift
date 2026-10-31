import SwiftUI

struct EOVWatchDial: View {
    let presentation: EOVWatchPresentation
    let refresh: () -> Void
    @State private var focus = 0
    @Environment(\.dynamicTypeSize) private var textSize
    var body: some View {
        let run = presentation.display
        let timeCaption = run.estimated ? (presentation.backgroundLimitLabel.map { "away · " + $0 } ?? "away time") : "run time"
        GeometryReader { viewport in
            // The original content needs135pt plus88pt of header/insets. Spend
            // additional height on the ring and lower row, without a device check.
            let spare = textSize.isAccessibilitySize ? 0 : max(0, min(28, viewport.size.height - 223))
            let diameter = 100 + spare * 0.7
            EOVWatchPanel(title: run.map, presentation: presentation, refresh: refresh, estimated: run.estimated) {
                VStack(spacing: 6 + spare * 0.3) {
                    Button { focus = (focus + 1) % 3 } label: {
                        ZStack {
                            EOVWatchRing(fraction: run.fraction, active: run.active)
                            VStack(spacing: 0) {
                                EOVWatchHero(presentation: presentation, size: 44, animate: presentation.heroCanAnimate)
                                if focus == 0 { EOVWatchDistance(distance: run.distance, cap: run.cap, reaping: run.reaping) }
                                else {
                                    Text(focus == 1 ? eovWatchNumber(run.resources) : run.estimated ? eovWatchDuration(presentation.away?.elapsed ?? 0) : eovWatchDuration(run.seconds))
                                        .eovWatchFont(26, weight: .bold, design: .rounded).monospacedDigit()
                                        .lineLimit(1).minimumScaleFactor(0.75)
                                    Text(focus == 1 ? (run.estimated ? "gained" : "resources") : timeCaption).eovWatchFont(10).foregroundStyle(EOVWatchPalette.muted)
                                }
                                HStack(spacing: 2) {
                                    EOVWatchGlyph(name: "Heart", size: 9)
                                    Text((run.estimated ? "Last " : "") + eovWatchNumber((run.health * 100).rounded()) + "%").eovWatchFont(9).foregroundStyle(EOVWatchPalette.muted)
                                }
                            }
                            // Keep the rectangular content inside the ring, including
                            // its narrower upper/lower arcs on the smaller Watch.
                            .frame(width: diameter - 20).scaleEffect(0.8)
                        }.frame(width: diameter, height: diameter)
                    }.buttonStyle(.plain).accessibilityLabel("Expedition dial. Tap to change the highlighted metric")
                    EOVWatchQuartet(run: run)
                }
            }
        }
    }
}


struct EOVWatchHealth: View {
    let presentation: EOVWatchPresentation
    let refresh: () -> Void
    var body: some View {
        let run = EOVWatchDisplay(presentation: presentation, useAway: false)
        EOVWatchPanel(title: presentation.ending ?? "Health", presentation: presentation, refresh: refresh, showSourceAge: false) {
            VStack(spacing: 5) {
                HStack(spacing: 4) {
                    EOVWatchHero(presentation: presentation, size: 55, animate: presentation.heroCanAnimate)
                    VStack(alignment: .leading, spacing: 1) {
                        HStack(alignment: .firstTextBaseline, spacing: 2) {
                            Text(eovWatchNumber((run.health * 100).rounded())).eovWatchFont(38, weight: .bold, design: .rounded)
                            Text("%").font(.caption2).foregroundStyle(EOVWatchPalette.muted)
                        }
                        Text("Phone health · " + presentation.ageLabel + " ago").eovWatchFont(10).foregroundStyle(EOVWatchPalette.muted)
                    }
                }
                GeometryReader { geo in
                    Capsule().fill(EOVWatchPalette.red.opacity(0.15))
                    Capsule().fill(run.health > 0.3 ? EOVWatchPalette.teal : EOVWatchPalette.red)
                        .frame(width: max(0, geo.size.width * run.health))
                }.frame(height: 7)
                HStack { Text(run.map); Spacer(); Text(eovWatchMetres(run.distance)) }.font(.caption2).foregroundStyle(EOVWatchPalette.muted)
                Spacer(minLength: 0)
                HStack {
                    EOVWatchMetric(art: "EOVKill", value: eovWatchNumber(presentation.snapshot?.run.map { Double($0.kills) }), label: "Actual run kills", big: true)
                    EOVWatchMetric(art: "EOVChest", value: eovWatchNumber(presentation.snapshot?.run?.resources), label: "Actual run resources", big: true)
                }
            }
        }
    }
}

struct EOVWatchResources: View {
    let presentation: EOVWatchPresentation
    let refresh: () -> Void
    @State private var averages = false
    var body: some View {
        let run = presentation.phone
        EOVWatchPanel(title: "Resources", presentation: presentation, refresh: refresh) {
            VStack(spacing: 5) {
                HStack(spacing: 5) {
                    EOVWatchGlyph(name: "EOVChest", size: 42)
                    VStack(alignment: .leading, spacing: 0) {
                        Text(averages ? eovWatchRate(run?.resources ?? 0, run?.durationSeconds ?? 0) : eovWatchNumber(run?.resources))
                            .eovWatchFont(36, weight: .bold, design: .rounded).monospacedDigit().lineLimit(1).minimumScaleFactor(0.6)
                        Text(averages ? "/min · Run average" : "run resources").eovWatchFont(10).foregroundStyle(EOVWatchPalette.muted)
                    }
                }
                HStack(spacing: 5) {
                    EOVWatchGain(art: "EOVTask", value: averages ? eovWatchRate(Double(run?.tasks ?? 0), run?.durationSeconds ?? 0) : eovWatchNumber(run.map { Double($0.tasks) }), label: averages ? "Tasks per minute, run average" : "Tasks")
                    EOVWatchGain(art: "EOVKill", value: averages ? eovWatchRate(Double(run?.kills ?? 0), run?.durationSeconds ?? 0) : eovWatchNumber(run.map { Double($0.kills) }), label: averages ? "Kills per minute, run average" : "Kills")
                }
                Button { averages.toggle() } label: {
                    HStack { Image(systemName: averages ? "number" : "speedometer"); Text(averages ? "Totals" : "Run averages") }.font(.caption2)
                        .frame(maxWidth: .infinity, minHeight: 44)
                }.buttonStyle(.plain).foregroundStyle(EOVWatchPalette.gold)
            }
        }
    }
}


struct EOVWatchSinceAway: View {
    let presentation: EOVWatchPresentation
    let refresh: () -> Void
    var body: some View {
        let run = presentation.display
        EOVWatchPanel(title: "Since you left", presentation: presentation, refresh: refresh, estimated: run.estimated) {
            if run.estimated {
                VStack(spacing: 3) {
                    HStack(alignment: .lastTextBaseline, spacing: 4) {
                        Text("Elapsed").eovWatchFont(10).foregroundStyle(EOVWatchPalette.muted)
                        Text(eovWatchDuration(presentation.away?.elapsed ?? 0)).eovWatchFont(27, weight: .bold, design: .rounded).monospacedDigit()
                            .lineLimit(1).minimumScaleFactor(0.65)
                        Spacer(minLength: 0)
                        VStack(alignment: .trailing, spacing: 0) {
                            if let label = presentation.backgroundLimitLabel {
                                Text(label).eovWatchFont(11, weight: .medium).fixedSize().foregroundStyle(EOVWatchPalette.gold)
                            }
                            if (run.reaps ?? 0) > 0 && run.phase == 0 && !run.expired { Text("New run").eovWatchFont(10).foregroundStyle(EOVWatchPalette.gold) }
                        }
                    }
                    HStack(spacing: 5) {
                        EOVWatchGain(art: "EOVTask", value: eovWatchNumber(run.tasks), label: "Task gains since departure")
                        EOVWatchGain(art: "EOVKill", value: eovWatchNumber(run.kills), label: "Kill gains since departure")
                    }
                    HStack(spacing: 5) {
                        EOVWatchGain(art: "EOVChest", value: "+" + eovWatchNumber(run.resources), label: "Resource gains since departure")
                        EOVWatchGain(art: "EOVCarl", value: run.reaps.map(String.init) ?? "—", label: "Reaps since departure")
                    }
                    HStack(alignment: .firstTextBaseline, spacing: 3) {
                        Text(eovWatchNumber(run.distance)).eovWatchFont(20, weight: .bold, design: .rounded)
                        Text(run.reaping ? "/ " + eovWatchNumber(run.cap) + " m" : "m").eovWatchFont(11).foregroundStyle(EOVWatchPalette.muted)
                    }.monospacedDigit()
                }
            } else {
                VStack(spacing: 10) {
                    EOVWatchHero(presentation: presentation, size: 42, animate: false)
                    Text("No away estimate yet").font(.headline)
                    Text("Your current run remains on iPhone.").font(.caption2).foregroundStyle(EOVWatchPalette.muted)
                }
            }
        }
    }
}


struct EOVWatchSkills: View {
    let presentation: EOVWatchPresentation
    let refresh: () -> Void
    @AppStorage("EOVWatch.PinnedSkill") private var selected = ""
    var body: some View {
        EOVWatchPanel(title: "Skillbook", presentation: presentation, refresh: refresh, showSourceAge: false) {
            Group {
                if let skills = presentation.snapshot?.skills, !skills.isEmpty {
                    let ordered = skills.filter { $0.id == selected } + skills.filter { $0.id != selected }
                    LazyVGrid(columns: [GridItem(.flexible()), GridItem(.flexible())], spacing: 3) {
                        ForEach(ordered) { skill in
                            Button { selected = selected == skill.id ? "" : skill.id } label: {
                                VStack(spacing: 1) {
                                    HStack(spacing: 4) {
                                        EOVWatchGlyph(name: eovWatchSkillArt(skill.id), size: 18)
                                        Text(String(skill.level)).eovWatchFont(21, weight: .bold, design: .rounded).monospacedDigit()
                                    }
                                    Text(skill.name).eovWatchFont(10).lineLimit(1).minimumScaleFactor(0.8)
                                }.frame(maxWidth: .infinity, minHeight: 44)
                                    .background(selected == skill.id ? EOVWatchPalette.gold.opacity(0.2) : EOVWatchPalette.panel, in: RoundedRectangle(cornerRadius: 12))
                                    .overlay(alignment: .topTrailing) { if selected == skill.id { Image(systemName: "pin.fill").eovWatchFont(9).padding(5).foregroundStyle(EOVWatchPalette.gold) } }
                            }.buttonStyle(.plain).accessibilityLabel(skill.name + ", level " + String(skill.level) + (selected == skill.id ? ", pinned" : ", tap to pin"))
                        }
                    }
                } else { Text("No skills yet").font(.caption) }
            }
        }
    }
}

struct EOVWatchHistory: View {
    let showReceipt: (EOVWatchSnapshot.RecentRun) -> Void
    let presentation: EOVWatchPresentation
    let refresh: () -> Void
    @State private var resources = false
    @State private var selectedNumber: Int?
    var body: some View {
        let runs = presentation.history.sorted { $0.runNumber < $1.runNumber }
        EOVWatchPanel(title: "Recent runs", presentation: presentation, refresh: refresh, showSourceAge: false) {
            if runs.isEmpty { Text("No recent runs").font(.caption) }
            else {
                let index = selectedNumber.flatMap { number in runs.firstIndex(where: { $0.runNumber == number }) } ?? (runs.count - 1)
                let selected = runs[index]
                let selection = Binding<Double>(get: { Double(index) }, set: { value in
                    selectedNumber = runs[min(runs.count - 1, max(0, Int(value.rounded())))].runNumber
                })
                VStack(spacing: 4) {
                    HStack {
                        EOVWatchGlyph(name: eovWatchOutcome(selected), size: 22)
                        VStack(alignment: .leading) {
                            Text(selected.mapTitle).font(.headline)
                            Text("#\(selected.runNumber) · \(selected.estimated ? "~ " : "")\(selected.result.capitalized)").eovWatchFont(10).foregroundStyle(EOVWatchPalette.muted)
                        }
                        Spacer(minLength: 0)
                        VStack(alignment: .trailing, spacing: 0) {
                            Text(eovWatchNumber(resources ? selected.resources : selected.distance)).eovWatchFont(26, weight: .bold, design: .rounded).monospacedDigit()
                            Text(resources ? "resources" : "metres").eovWatchFont(9).foregroundStyle(EOVWatchPalette.muted)
                        }
                    }
                    EOVWatchBars(runs: runs, resources: resources, selection: selection).frame(height: 45)
                        .focusable().digitalCrownRotation(selection, from: 0, through: Double(max(1, runs.count - 1)), by: 1, sensitivity: .medium, isContinuous: false, isHapticFeedbackEnabled: true)
                    HStack(spacing: 1) {
                        Button { resources.toggle() } label: { HStack(spacing: 3) { EOVWatchGlyph(name: resources ? "EOVChest" : "EOVHero", size: 16); Text(resources ? "Resources" : "Distance").eovWatchFont(10) }.frame(maxWidth: .infinity, minHeight: 44) }
                            .accessibilityLabel("Change chart metric")
                        Button { showReceipt(selected) } label: { Image(systemName: "chevron.right").frame(width: 44, height: 44) }.accessibilityLabel("Open selected run receipt")
                    }.buttonStyle(.plain).foregroundStyle(EOVWatchPalette.gold)
                }
            }
        }
    }
}
struct EOVWatchBars: View {
    let runs: [EOVWatchSnapshot.RecentRun]
    let resources: Bool
    @Binding var selection: Double
    private var values: [Double] { runs.map { resources ? $0.resources : $0.distance } }
    private var maximum: Double { max(1.0, values.max() ?? 1.0) }
    private func bar(_ index: Int, height: CGFloat) -> some View {
        let selected = Int(selection.rounded()) == index
        let tone: Color = selected ? EOVWatchPalette.gold : (runs[index].estimated ? EOVWatchPalette.teal.opacity(0.38) : EOVWatchPalette.teal)
        let barHeight = max(CGFloat(0), (height - 13) * CGFloat(values[index] / maximum))
        return VStack(spacing: 2) {
            if runs[index].estimated { Text("~").eovWatchFont(9).foregroundStyle(EOVWatchPalette.muted) }
            RoundedRectangle(cornerRadius: 3).fill(tone).frame(height: barHeight)
        }.frame(maxWidth: .infinity)
    }
    var body: some View {
        GeometryReader { geo in
            ZStack(alignment: .bottom) {
                Rectangle().fill(EOVWatchPalette.muted.opacity(0.2)).frame(height: 1)
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

struct EOVWatchComparison: View {
    let showReceipt: (EOVWatchSnapshot.RecentRun) -> Void
    let presentation: EOVWatchPresentation
    let refresh: () -> Void
    @State private var selected = 0
    var body: some View {
        let all = presentation.history
        let map = presentation.phone?.mapId ?? all.first?.mapId
        let same = all.filter { $0.mapId == map }
        let runs = same.count >= 2 ? same : all
        EOVWatchPanel(title: "Compare runs", presentation: presentation, refresh: refresh, showSourceAge: false) {
            if runs.count < 2 { Text("Two recent runs needed").font(.caption) }
            else {
                let first = runs[selected % runs.count]
                let second = runs[(selected + 1) % runs.count]
                let maximum = max(rawRate(first), rawRate(second))
                let unit = maximum >= 1e9 ? 1e9 : maximum >= 1e6 ? 1e6 : maximum >= 1e3 ? 1e3 : 1
                VStack(spacing: 1) {
                    let delta = eovWatchRateValue(first.resources, first.durationSeconds) != nil && eovWatchRateValue(second.resources, second.durationSeconds) != nil ? (rawRate(first) / unit).rounded() * unit - (rawRate(second) / unit).rounded() * unit : nil
                    HStack(spacing: 3) {
                        EOVWatchGlyph(name: "EOVChest", size: 17)
                        VStack(alignment: .leading, spacing: 0) {
                            Text(delta.map { (first.estimated || second.estimated ? "~ " : "") + ($0 >= 0 ? "+" : "−") + eovWatchNumber(abs($0)) } ?? "—")
                                .eovWatchFont(20, weight: .bold, design: .rounded).monospacedDigit().lineLimit(1)
                            Text("resources / min").eovWatchFont(9).foregroundStyle(EOVWatchPalette.muted)
                        }
                        Spacer(minLength: 0)
                        Button { selected = (selected + 1) % runs.count } label: { Image(systemName: "arrow.left.arrow.right").frame(width: 44, height: 44) }
                            .buttonStyle(.plain).accessibilityLabel("Choose the next pair of recent runs")
                    }.foregroundStyle(EOVWatchPalette.gold)
                    comparisonRow(first, tone: EOVWatchPalette.gold, unit: unit, maximum: maximum)
                    comparisonRow(second, tone: EOVWatchPalette.teal, unit: unit, maximum: maximum)
                }
            }
        }
    }
    private func rawRate(_ run: EOVWatchSnapshot.RecentRun) -> Double { eovWatchRateValue(run.resources, run.durationSeconds) ?? 0 }
    private func comparisonRow(_ run: EOVWatchSnapshot.RecentRun, tone: Color, unit: Double, maximum: Double) -> some View {
        let displayedRate = run.durationSeconds > 0 ? eovWatchNumber((rawRate(run) / unit).rounded() * unit) : "—"
        return Button { showReceipt(run) } label: {
            HStack(spacing: 5) {
                EOVWatchGlyph(name: eovWatchMapArt(run.mapId), size: 22)
                VStack(alignment: .leading, spacing: 2) {
                    Text("\(run.mapTitle) #\(run.runNumber) · \(run.estimated ? "~ " : "")\(run.result.capitalized)").eovWatchFont(10, weight: .semibold).lineLimit(1)
                    HStack(spacing: 3) {
                        Text(eovWatchDuration(run.durationSeconds)); Text("·"); Text(eovWatchNumber(run.distance) + " m"); Spacer(minLength: 0); Text(displayedRate + "/min")
                    }.eovWatchFont(10).monospacedDigit()
                }
            }.padding(.horizontal, 5).frame(minHeight: 44).background(tone.opacity(0.08), in: RoundedRectangle(cornerRadius: 10))
                .overlay(alignment: .bottomLeading) {
                    GeometryReader { geo in Capsule().fill(tone).frame(width: geo.size.width * CGFloat(min(1, rawRate(run) / max(1, maximum)))) }.frame(height: 2)
                }
        }.buttonStyle(.plain)
    }
}


struct EOVWatchShell: View {
    let presentation: EOVWatchPresentation
    @Binding var screen: EOVWatchScreen
    let refresh: () -> Void
    @State private var browsing = false
    @State private var receipt: EOVWatchSnapshot.RecentRun?
    var body: some View {
        let resolved = presentation.resolved(screen)
        Group {
            if [.dial, .health, .resources, .away].contains(resolved), presentation.phone == nil {
                EOVWatchEmpty(presentation: presentation, refresh: refresh)
            } else {
                switch resolved {
                case .automatic, .dial: EOVWatchDial(presentation: presentation, refresh: refresh)
                case .health: EOVWatchHealth(presentation: presentation, refresh: refresh)
                case .resources: EOVWatchResources(presentation: presentation, refresh: refresh)
                case .away: EOVWatchSinceAway(presentation: presentation, refresh: refresh)
                case .skills: EOVWatchSkills(presentation: presentation, refresh: refresh)
                case .history: EOVWatchHistory(showReceipt: { receipt = $0 }, presentation: presentation, refresh: refresh)
                case .comparison: EOVWatchComparison(showReceipt: { receipt = $0 }, presentation: presentation, refresh: refresh)
                }
            }
        }
        .background(EOVWatchPalette.ink.ignoresSafeArea())
        .overlay(alignment: .topLeading) {
            Button { browsing = true } label: {
                Image(systemName: "line.3.horizontal").font(.system(size: 17, weight: .semibold))
                    .frame(width: 44, height: 44).contentShape(Rectangle())
            }.buttonStyle(.plain).foregroundStyle(EOVWatchPalette.gold).offset(x: 3, y: 0)
                .accessibilityLabel("Browse Run, Skills and History")
        }
        .ignoresSafeArea().navigationTitle("").toolbar(.hidden, for: .navigationBar)
        .sheet(isPresented: $browsing) {
            NavigationStack {
                List {
                    Section("Run") { choices([.automatic, .dial, .health, .resources, .away]) }
                    Section("Skills") { choices([.skills]) }
                    Section("History") { choices([.history, .comparison]) }
                    Section("iPhone") {
                        Button("Refresh from iPhone", systemImage: "arrow.clockwise") { refresh(); browsing = false }
                        Text(presentation.source()).font(.caption2).foregroundStyle(.secondary)
                        if !presentation.refreshDescription.isEmpty { Text(presentation.refreshDescription).font(.caption2) }
                        Text("Away estimates update here until their limit. Open Echoes for fresh game data.").font(.caption2).foregroundStyle(.secondary)
                    }
                }.navigationTitle("Echoes").toolbar(.visible, for: .navigationBar)
            }.tint(EOVWatchPalette.gold)
        }
        .sheet(item: $receipt) { run in
            if let current = presentation.history.first(where: { $0.runNumber == run.runNumber }) {
                EOVWatchReceipt(run: current, presentation: presentation, refresh: refresh).tint(EOVWatchPalette.gold)
            } else {
                EOVWatchPanel(title: "Run #" + String(run.runNumber), presentation: presentation, refresh: refresh) {
                    Text("This run is no longer in the retained history.").font(.caption).multilineTextAlignment(.center)
                }
            }
        }
    }
    @ViewBuilder private func choices(_ screens: [EOVWatchScreen]) -> some View {
        ForEach(screens, id: \.rawValue) { destination in
            Button(destination.title) { screen = destination; browsing = false }.frame(minHeight: 44)
        }
    }
}
struct EOVWatchEmpty: View {
    let presentation: EOVWatchPresentation
    let refresh: () -> Void
    var body: some View {
        EOVWatchPanel(title: "Echoes", presentation: presentation, refresh: refresh) {
            VStack(spacing: 10) {
                EOVWatchHero(presentation: presentation, size: 42, animate: false)
                Text(presentation.snapshot?.state == "town" ? "Between adventures" : presentation.snapshot?.state == "loading" ? "Loading on iPhone" : "No snapshot yet").font(.headline).multilineTextAlignment(.center)
                Text("Open Echoes on iPhone").font(.caption2).foregroundStyle(EOVWatchPalette.muted)
                if !presentation.refreshDescription.isEmpty { Text(presentation.refreshDescription).font(.caption2).foregroundStyle(EOVWatchPalette.muted).multilineTextAlignment(.center) }
            }
        }
    }
}
struct EOVWatchReceipt: View {
    let run: EOVWatchSnapshot.RecentRun
    let presentation: EOVWatchPresentation
    let refresh: () -> Void
    var body: some View {
        EOVWatchPanel(title: "Run #" + String(run.runNumber), presentation: presentation, refresh: refresh) {
            VStack(spacing: 8) {
                EOVWatchGlyph(name: eovWatchOutcome(run), size: 32)
                Text(run.mapTitle).font(.headline)
                Text((run.estimated ? "Estimated · " : "Recorded · ") + run.result.capitalized).font(.caption2).foregroundStyle(EOVWatchPalette.muted)
                EOVWatchMetric(art: "EOVChest", value: eovWatchNumber(run.resources), label: "Run resources", big: true)
                HStack { Text(eovWatchMetres(run.distance)); Spacer(); Text(eovWatchDuration(run.durationSeconds)) }.font(.caption2).monospacedDigit()
                HStack {
                    EOVWatchMetric(art: "EOVTask", value: eovWatchNumber(Double(run.tasks)), label: "Run tasks")
                    EOVWatchMetric(art: "EOVKill", value: eovWatchNumber(Double(run.kills)), label: "Run kills")
                }
                Text(eovWatchRate(run.resources, run.durationSeconds) + " resources/min · run average").font(.caption2).foregroundStyle(EOVWatchPalette.muted)
            }
        }.background(EOVWatchPalette.ink.ignoresSafeArea())
    }
}
