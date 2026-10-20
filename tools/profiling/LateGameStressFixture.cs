// Copy into Assets/Editor in an isolated profiling checkout; never runs in a shipping player.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Blindsided.SaveData;
using Blindsided.Utilities;
using Newtonsoft.Json;
using TimelessEchoes.Buffs;
using TimelessEchoes.Characters;
using TimelessEchoes.Farming;
using TimelessEchoes.Gear;
using TimelessEchoes.Quests;
using TimelessEchoes.Skills;
using TimelessEchoes.Upgrades;
using UnityEditor;
using UnityEngine;
public static class LateGameStressFixture
{
 public const double Balance=1e12;
 public static void Build()
 {
  var source=Environment.GetEnvironmentVariable("EOV_STRESS_SOURCE");
  var target=Environment.GetEnvironmentVariable("EOV_STRESS_DESTINATION");
  if(string.IsNullOrEmpty(source)||string.IsNullOrEmpty(target)||Directory.Exists(target)||!target.Contains("Artifacts"))throw new Exception("Require copied source and new isolated Artifacts target");
  var root=typeof(SaveManager).GetField("rootPathOverride",BindingFlags.NonPublic|BindingFlags.Static);var old=root.GetValue(null);
  var resources=AssetCache.GetAll<Resource>();var buffs=BuffRecipe.LoadAvailable();
  var skills=AssetDatabase.FindAssets("t:Skill").Select(g=>AssetDatabase.LoadAssetAtPath<Skill>(AssetDatabase.GUIDToAssetPath(g))).Where(s=>s).ToArray();
  var quests=AssetCache.GetAll<QuestData>();var config=Resources.Load<CauldronConfig>("Cauldron/CauldronConfig");
  var rarity=AssetCache.GetAll<RaritySO>().OrderByDescending(r=>r.tierIndex).First();var stats=AssetCache.GetAll<StatDefSO>();
  var core=AssetCache.GetAll<CoreSO>().OrderByDescending(c=>c.tierIndex).First();
  var farm=FarmContent.Load();
  Directory.CreateDirectory(target);
  try {foreach(var mode in new[]{"eternal","ordinary"}) {
   root.SetValue(null,source);var loaded=SaveManager.Instance.LoadDetailedAsync("Save1").GetAwaiter().GetResult();
   if(!loaded.Succeeded)throw new Exception(loaded.Diagnostic);
   var d=loaded.Data;
   // These are explicit synthetic starting conditions, never claimed as earned player progress.
   d.Character=CharacterProfile.Legacy();d.CraftingMasteryLevel=1000;d.CraftingMasteryXP=0;d.CauldronEvaLevel=1000;d.CauldronEvaXp=0;d.CauldronStew=Balance;
   foreach(var r in resources)d.Resources[r.name]=new GameData.ResourceEntry{Amount=Balance,Earned=true,Tier=8};
   foreach(var q in quests)if(q)d.Quests[q.name]=new GameData.QuestRecord{Accepted=true,Completed=true};
   d.SkillData.Clear();foreach(var skill in skills) {
    var progress=new GameData.SkillProgress{Level=1000,CurrentXP=0};
    foreach(var m in skill.milestones)if(m)progress.Milestones.Add(new GameData.MilestoneProgressRecord{Id=m.Id,TierIndex=m.GetTierIndex(1000),IsActive=m.CanActivate&&m.ActiveEffect is MilestoneCauldronTasteBonusEffectDefinition});
    d.SkillData[skill.name]=progress;
   }
   d.CauldronCardCounts.Clear();
   foreach(var r in resources)if(!r.DisableAlterEcho)d.CauldronCardCounts["RES:"+r.name]=mode=="eternal"?config.resourceTierThresholds.Last():config.resourceTierThresholds.Last()/2;
   foreach(var b in buffs)d.CauldronCardCounts["BUFF:"+b.name]=mode=="eternal"?config.buffTierThresholds.Last():config.buffTierThresholds.Last()/2;
   foreach(var inf in AssetCache.GetAll<InfinityCauldronStatSO>("Infinity"))d.CauldronCardCounts["INF:"+inf.Stat]=mode=="eternal"?1000000:0;
   d.EquipmentBySlot.Clear();
   foreach(var slot in new[]{"Weapon","Helmet","Chest","Boots"}) {
    var guaranteed=slot=="Boots"?HeroStatMapping.MoveSpeed:slot=="Chest"?HeroStatMapping.Defense:slot=="Helmet"?HeroStatMapping.MaxHealth:HeroStatMapping.Damage;
    var eligible=stats.Where(s=>s&&(slot=="Boots"||s.heroMapping!=HeroStatMapping.MoveSpeed)).OrderByDescending(s=>s.heroMapping==guaranteed).ThenByDescending(s=>s.maxRoll*s.ComparisonScale).Take(rarity.affixCount);
    d.EquipmentBySlot[slot]=new GearItemRecord{slot=slot,rarity=rarity.name,affixes=eligible.Select(s=>new GearAffixRecord{statId=s.id,quality=1,value=s.RemapRoll(1)}).ToList()};
   }
   d.SavedPreferences.LastSelectedForgeCore=core.name;d.SavedPreferences.LastSelectedForgeSlot="Weapon";d.SavedPreferences.LockAutocraftStatSet=false;
   // Farm gameplay constructs paid, journalled batches after load through normal service APIs.
   d.Farm=new FarmState{ProductionRevision=1,TwinsLevel=60,GardenCapacity=6,OrchardCapacity=6};
   foreach(var id in FarmCommands.GardenBeds.Concat(FarmCommands.OrchardBeds))d.Farm.Beds[id]=new FarmBedState{Unlocked=true,Repeat=true};
   d.Quests[FarmContent.IntroductionId]=new GameData.QuestRecord{Accepted=true,Completed=true};
   foreach(var build in farm.builds)d.Quests[build.questId]=new GameData.QuestRecord{Accepted=true,Completed=true};
   var folder=Path.Combine(target,mode);root.SetValue(null,folder);
   var result=SaveManager.Instance.ReplaceSlotDetailedAsync(d,"Save1").GetAwaiter().GetResult();if(!result.Succeeded)throw new Exception(result.Error);
   var roundtrip=SaveManager.Instance.LoadDetailedAsync("Save1").GetAwaiter().GetResult();if(!roundtrip.Succeeded||!roundtrip.IntegrityVerified)throw new Exception("Fixture roundtrip invalid");
   File.WriteAllText(Path.Combine(folder,"fixture.json"),JsonConvert.SerializeObject(new{synthetic=true,mode,resourceCount=d.Resources.Count,balance=Balance,skills=d.SkillData.ToDictionary(p=>p.Key,p=>p.Value.Level),d.CraftingMasteryLevel,d.CauldronEvaLevel,d.CauldronStew,core=core.name,coreTier=core.tierIndex,rarity=rarity.name,affixes=rarity.affixCount,resourceCards=config.resourceTierThresholds.Last(),buffCards=config.buffTierThresholds.Last(),ordinaryCards=d.CauldronCardCounts.Count,farms=12,twinsLevel=60,roundtrip.IntegrityVerified},Formatting.Indented));
  }}finally{root.SetValue(null,old);}
 }
}
