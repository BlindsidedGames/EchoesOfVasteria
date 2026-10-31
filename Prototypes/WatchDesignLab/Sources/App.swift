import SwiftUI

@main
struct EchoesWatchDesignLab: App {
    @StateObject private var lab = LabStore()
    var body: some Scene {
        WindowGroup {
            NavigationStack { DesignRoot().environmentObject(lab) }
                .tint(Palette.gold)
                .environment(\.dynamicTypeSize, lab.largeText ? .accessibility1 : .large)
        }
    }
}

struct DesignRoot: View {
    @EnvironmentObject var lab: LabStore
    var body: some View {
        Group {
            if lab.snapshot == nil || (lab.design <= 6 && lab.snapshot?.state != "run") { EmptyExpedition() }
            else {
                switch lab.design {
                case 2: LivingTrail()
                case 3: CombatWatch()
                case 4: HarvestCounter()
                case 5: SinceAway()
                case 6: RunGoal()
                case 7: SkillBook()
                case 8: RunRibbon()
                case 9: RunComparison()
                case 10: MapDossier()
                default: ExpeditionDial()
                }
            }
        }
        .background(Palette.ink)
        .navigationTitle("").toolbarTitleDisplayMode(.inline)
        .preferredColorScheme(.dark)
    }
}
