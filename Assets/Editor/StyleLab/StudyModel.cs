// Editor-only, in-memory fixtures. No Oracle, managers, PlayerPrefs, saves or cloud calls.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using TimelessEchoes.Buffs;
using TimelessEchoes.Tasks;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Upgrades.Cauldron;
using TimelessEchoes.UI.Toolkit;
using TimelessEchoes.UI.Cauldron;

namespace Vasteria.StyleLab
{
    internal sealed class StudyModel
    {
        public readonly ToolkitCauldronDefinition Definition = AssetDatabase.LoadAssetAtPath<ToolkitCauldronDefinition>("Assets/UI/Toolkit/Cauldron.asset");
        public readonly ToolkitRunDefinition Run = AssetDatabase.LoadAssetAtPath<ToolkitRunDefinition>("Assets/UI/Toolkit/Run.asset");
        public CauldronConfig Config => Definition.config;
        public readonly Resource[] Resources, Foods;
        public readonly BuffRecipe[] Buffs;
        public readonly InfinityCauldronStatSO[] Boons;
        public readonly Dictionary<string, int> Counts = new();
        public readonly Dictionary<Resource, double> Amounts = new();
        public readonly Dictionary<Resource, string> Groups = new();
        public readonly CardTierCalculator Tiers;
        public double Stew = 21400000;
        public int EvaLevel = 733, Tastes = 1248;
        public bool Tasting = true;
        public bool[] AutoCast = { true, true, false, true, false };
        public int HealthState;
        public bool ReturnOnDeath = true;
        public Resource First, Second;
        public bool ReplaceFirst = true;
        public string Fixture;
        public bool InfinityActive => Resources.Where(r => !r.DisableAlterEcho).All(r => Tiers.IsMaxed("RES:" + r.name)) && Buffs.All(b => Tiers.IsMaxed("BUFF:" + b.name));
        public StudyModel(string fixture = "mixed")
        {
            Resources = Load<Resource>("Assets/Resources/Resource Items").Where(r => r.icon).OrderBy(r => r.resourceID).ThenBy(r => r.name).ToArray();
            Buffs = Load<BuffRecipe>("Assets/Resources/Buffs");
            Boons = Load<InfinityCauldronStatSO>("Assets/Resources/Infinity");
            var classifier = new AEResourceGroupClassifier();
            foreach (var r in Resources) Groups[r] = CauldronCollectionPresentation.FormatGroupName(classifier.Classify(r));
            var eligible = new HashSet<Resource>();
            foreach (var task in Load<TaskData>("Assets/Resources/Tasks"))
            {
                string skill = task.associatedSkill ? task.associatedSkill.name : "";
                if (skill.IndexOf("farm", StringComparison.OrdinalIgnoreCase) < 0 && skill.IndexOf("fish", StringComparison.OrdinalIgnoreCase) < 0) continue;
                foreach (var drop in task.resourceDrops) if (drop?.resource) eligible.Add(drop.resource);
            }
            foreach (var r in Resources) if (r.cauldronCategory == Resource.CauldronCategory.Farming || r.cauldronCategory == Resource.CauldronCategory.Fishing) eligible.Add(r);
            Foods = Resources.Where(eligible.Contains).ToArray();
            Tiers = new CardTierCalculator(Config, () => Counts);
            Reset(fixture);
        }
        private static T[] Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { path }).Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g))).Where(x => x).ToArray();
        public void Reset(string fixture)
        {
            Fixture = fixture; Counts.Clear(); Amounts.Clear(); First = Second = null; ReplaceFirst = true;
            Stew = fixture == "empty" ? 0 : 21400000; Tasting = Stew > 0;
            for (int i = 0; i < Resources.Length; i++)
            {
                var r = Resources[i]; Amounts[r] = fixture == "empty" ? 0 : 120000 + (i + 1) * 237000;
                Counts["RES:" + r.name] = fixture == "maxed" ? Config.resourceTierThresholds.Last() : fixture == "empty" ? 0 : SampleCount(Config.resourceTierThresholds, i);
            }
            for (int i = 0; i < Buffs.Length; i++) Counts["BUFF:" + Buffs[i].name] = fixture == "maxed" ? Config.buffTierThresholds.Last() : fixture == "empty" ? 0 : SampleCount(Config.buffTierThresholds, i);
            for (int i = 0; i < Boons.Length; i++) Counts["INF:" + Boons[i].Stat] = fixture == "maxed" ? 400 + i * 143 : 0;
            if (fixture != "empty") { First = Foods.FirstOrDefault(r => r.name == "Carrot"); Second = Foods.FirstOrDefault(r => r.name == "Wheat"); }
        }
        private static int SampleCount(int[] limits, int index) { int tier = index % limits.Length; return tier == limits.Length - 1 ? limits[tier] : limits[tier] + (limits[tier + 1] - limits[tier]) / 3; }
        public void Select(Resource food)
        {
            if (Amounts[food] <= 0) return;
            if (First == food) { First = null; return; }
            if (Second == food) { Second = null; return; }
            if (!First) { First = food; ReplaceFirst = false; }
            else if (!Second) { Second = food; ReplaceFirst = true; }
            else { if (ReplaceFirst) First = food; else Second = food; ReplaceFirst = !ReplaceFirst; }
        }
        public double Value(Resource r) => r ? Amounts[r] * r.baseValue * r.valueMultiplier / 100d : 0;
        public bool CanMix => First && Second && First != Second && Amounts[First] > 0 && Amounts[Second] > 0;
        public Resource[] MixTargets(bool all) => all ? Foods.Where(r => Amounts[r] > 0).Take(Foods.Count(r => Amounts[r] > 0) / 2 * 2).ToArray() : CanMix ? new[] { First, Second } : Array.Empty<Resource>();
        public void Mix(Resource[] targets) { Stew += targets.Sum(Value); foreach (var r in targets) Amounts[r] = 0; First = Second = null; Tasting = Stew >= Config.stewPerRoll; }
        public int Tier(string id) => Tiers.GetTier(id);
        public int Count(string id) => Counts.TryGetValue(id, out int value) ? value : 0;
        public int Next(string id) { var a = id.StartsWith("RES:") ? Config.resourceTierThresholds : Config.buffTierThresholds; return a[Mathf.Clamp(Tier(id), 0, a.Length - 1)]; }
        public int GroupTier(IEnumerable<string> ids) => ids.Any() ? ids.Min(Tier) : 0;
        public List<(string key, float weight, Color color)> Odds()
        {
            bool active = InfinityActive;
            var c = Config; int l = EvaLevel;
            bool Has(string group) => Resources.Any(r => !r.DisableAlterEcho && Groups[r] == group && !Tiers.IsMaxed("RES:" + r.name));
            var list = new List<(string,float,Color)> {
                ("nothing",c.weightNothing.Evaluate(l),c.sliceNothing),
                ("Farming",Has("Farming")?c.weightAEFarming.Evaluate(l):0,c.sliceAEFarming),
                ("Fishing",Has("Fishing")?c.weightAEFishing.Evaluate(l):0,c.sliceAEFishing),
                ("Mining",Has("Mining")?c.weightAEMining.Evaluate(l):0,c.sliceAEMining),
                ("Logging",Has("Logging")?c.weightAEWoodcutting.Evaluate(l):0,c.sliceAEWoodcutting),
                ("Combat",Has("Combat")?c.weightAECombat.Evaluate(l):0,c.sliceAECombat),
                ("Looting",Has("Looting")?c.weightAELooting.Evaluate(l):0,c.sliceAELooting),
                ("buffs",Buffs.Any(b=>!Tiers.IsMaxed("BUFF:"+b.name))?c.weightBuffCard.Evaluate(l):0,c.sliceBuff),
                ("lowest",active?0:c.weightLowestCountCard.Evaluate(l),c.sliceLowest),
                ("blessing",c.weightEvasBlessingX2.Evaluate(l),c.sliceEvas),
                ("surge",c.weightVastSurgeX10.Evaluate(l),c.sliceVast),
                ("eternal",active?c.weightInfinity.Evaluate(l):0,c.sliceInfinity)
            };
            return list.Where(x => x.Item2 > 0).ToList();
        }
    }
}
