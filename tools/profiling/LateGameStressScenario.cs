// Player-only observer. Copy into an isolated profiling checkout, never the shipping Assets tree.
#if (EOV_STANDALONE_PROFILE || EOV_MEMORY_PROFILE) && !UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using Blindsided;
using Newtonsoft.Json;
using TimelessEchoes.Farming;
using TimelessEchoes.Gear;
using TimelessEchoes.Skills;
using TimelessEchoes.UI;
using TimelessEchoes.UI.Toolkit;
using TimelessEchoes.Upgrades;
using UnityEngine;
using UnityEngine.Profiling;
using Object=UnityEngine.Object;
public static class LateGameStressScenario
{
 static readonly BindingFlags PrivateInstance=BindingFlags.Instance|BindingFlags.NonPublic;
 public static IEnumerator Run(string output,Action<string> mark,Action<string> inventory,Action finish)
 {
  string mode=Environment.GetEnvironmentVariable("EOV_STRESS_MODE")??"eternal";
  float phaseSeconds=float.TryParse(Environment.GetEnvironmentVariable("EOV_STRESS_PHASE_SECONDS"),out var secs)?Mathf.Max(3,secs):120;
  var deadline=Time.realtimeSinceStartup+30;
  while(!(TownWindowManager.Instance&&FarmService.Instance&&FarmService.Instance.Ready&&TimelessEchoes.GameManager.Instance.IsInTown)){
   if(Time.realtimeSinceStartup>deadline)throw new Exception("Stress setup: town services did not become ready");yield return null;
  }
  var d=Oracle.oracle.saveData;var windows=TownWindowManager.Instance;var cauldron=CauldronManager.Instance;var farm=FarmService.Instance;
  if(d.CraftingMasteryLevel<1000||d.CauldronEvaLevel<1000||d.CauldronStew<1e11)throw new Exception("High-state fixture was not loaded");
  var startupTastings=d.CauldronTotals.TotalTastings;var startupActive=cauldron.IsTasting;
  cauldron.StopTasting();windows.CloseAllWindows();
  Blindsided.SaveData.StaticReferences.StopAutocraftOnVastium=false;
  Blindsided.SaveData.StaticReferences.LockAutocraftStatSet=false;
  Blindsided.SaveData.StaticReferences.FastCraftMode=false;
  windows.OpenWindow(TownWindowManager.Window.Forge);yield return null;
  var session=Object.FindAnyObjectByType<ForgeSession>();
  if(!session||!session.Core||session.Core.tierIndex!=7||!session.CanCraft())throw new Exception("Highest core/affordability not available");
  var rates=CraftingService.Instance.Config;var controller=SkillController.Instance;
  var actualSpeed=(int)typeof(ForgeSession).GetMethod("GetCurrentCraftsPerSecond",PrivateInstance).Invoke(session,null);
  var resourceManager=ResourceManager.Instance;var binding=session.Catalog.Find(session.Core);
  if(resourceManager.GetAmount(binding.coreResource)<1e11||resourceManager.GetAmount(binding.ingotResource)<1e11)throw new Exception("Large crafting balances absent");
  if((mode=="eternal")!=cauldron.IsInfinityActive())throw new Exception("Cauldron state does not match fixture "+mode);
  // All batches go through ordinary paid journalled commands. Only the initial growth age is synthetic.
  int planted=0;
  foreach(var id in FarmCommands.GardenBeds.Concat(FarmCommands.OrchardBeds)){
   bool orchard=FarmCommands.OrchardBeds.Contains(id);
   var recipes=farm.Content.recipes.Where(r=>r!=null&&r.orchard==orchard&&farm.Content.CanPlant(d,r)).ToArray();
   if(recipes.Length==0)throw new Exception("No eligible farm recipe");
   var recipe=recipes[planted%recipes.Length];
   if(!farm.Plant(id,recipe.id)||!farm.Water(id)||!farm.SetRepeat(id,true))throw new Exception("Normal farm setup failed: "+id+" / "+farm.LastError);
   planted++;yield return null;
  }
  FarmCommands.TickFields(d.Farm,300); // Valid pre-aged synthetic starting state, not an altered production clock/rate.
  windows.CloseAllWindows();
  File.WriteAllText(Path.Combine(output,"stress-contract.json"),JsonConvert.SerializeObject(new{
   synthetic=true,mode,phaseSeconds,core=session.Core.name,coreTier=session.Core.tierIndex,slot=session.Slot,
   d.CraftingMasteryLevel,d.CauldronEvaLevel,d.CauldronStew,resources=d.Resources.Count,minResourceBalance=d.Resources.Values.Min(x=>x.Amount),
   skills=d.SkillData.ToDictionary(p=>p.Key,p=>p.Value.Level),actualCraftsPerSecond=actualSpeed,rates.baseCraftsPerSecond,rates.maxCraftsPerSecond,rates.batchInterval,
   cauldronRollsPerSecond=Resources.Load<CauldronConfig>("Cauldron/CauldronConfig").rollsPerSecond,
   tasteBonus=controller.Aggregator.GetCauldronTasteBonus(),costPerTaste=cauldron.GetStewCostPerRoll(),infinityActive=cauldron.IsInfinityActive(),
   startupTastings,startupActive,cardIds=d.CauldronCardCounts.Count,cardCopies=d.CauldronCardCounts.Values.Sum(x=>(long)x),
   planted,farm.Tuning,syntheticInitialGrowthAgeSeconds=300,initialGardenCapacity=d.Farm.GardenCapacity,initialOrchardCapacity=d.Farm.OrchardCapacity,
   normalUpgradeStopsPreserved=true,stopOnVastium=false,lockStats=false,fastCraftMode=false
  },Formatting.Indented));
  mark("stress-idle-control");yield return new WaitForSecondsRealtime(Mathf.Min(15,phaseSeconds));inventory("stress-idle-control");
  int initialCrafts=d.Forge.AutocraftCrafts,initialTastes=d.CauldronTotals.TotalTastings;long initialSequence=d.Farm.CommittedThroughSequence;
  int restarts=0;float began=Time.realtimeSinceStartup;
  session.StartAutoCrafting();cauldron.StartTasting();
  using(var writer=new StreamWriter(Path.Combine(output,"activity.csv"))){
   writer.WriteLine("seconds,phase,crafting,tasting,autoCrafts,totalCrafts,tastings,cards,stew,coreBalance,ingotBalance,infinityActive,telemetryPending,farmSequence,planted,ready,allocated,managedUsed,graphics,restarts");
   foreach(var phase in new[]{"hidden","Forge","CauldronTasting","CauldronCollection","Farm"}) {
    windows.CloseAllWindows();
    if(phase=="Forge")windows.OpenWindow(TownWindowManager.Window.Forge);
    if(phase.StartsWith("Cauldron")){
     windows.OpenWindow(TownWindowManager.Window.Cauldron);
     var screen=Object.FindAnyObjectByType<ToolkitCauldronScreen>();
     typeof(ToolkitCauldronScreen).GetField("mobileTab",PrivateInstance).SetValue(screen,phase=="CauldronTasting"?"Tasting":"Collection");
     typeof(ToolkitCauldronScreen).GetMethod("ApplyMobileTabs",PrivateInstance).Invoke(screen,null);
     if(phase=="CauldronCollection"){
      var collections=typeof(ToolkitCauldronScreen).GetField("collections",PrivateInstance).GetValue(screen);var t=collections.GetType();
      t.GetField("activeTab",PrivateInstance).SetValue(collections,mode=="eternal"?"Eternal":"Resources");t.GetMethod("SelectTab",PrivateInstance).Invoke(collections,null);
     }
    }
    if(phase=="Farm")windows.OpenWindow(TownWindowManager.Window.Farm);
    mark("stress-"+phase);float end=Time.realtimeSinceStartup+phaseSeconds;
    int beforeCraft=d.Forge.AutocraftCrafts,beforeTaste=d.CauldronTotals.TotalTastings;
    while(Time.realtimeSinceStartup<end){
     if(!session.IsAutoCrafting){restarts++;File.AppendAllText(Path.Combine(output,"natural-stops.jsonl"),JsonConvert.SerializeObject(new{seconds=Time.realtimeSinceStartup,phase,restarts,reasons=d.Forge.AutocraftStopReasons})+"\n");session.StartAutoCrafting();}
     if(!cauldron.IsTasting)throw new Exception("Tasting stopped despite ample stew");
     writer.WriteLine(FormattableString.Invariant($"{Time.realtimeSinceStartup:F3},{phase},{session.IsAutoCrafting},{cauldron.IsTasting},{d.Forge.AutocraftCrafts},{d.Forge.TotalCrafts},{d.CauldronTotals.TotalTastings},{d.CauldronTotals.TotalCards},{cauldron.Stew:R},{resourceManager.GetAmount(binding.coreResource):R},{resourceManager.GetAmount(binding.ingotResource):R},{cauldron.IsInfinityActive()},{BackgroundTelemetryProcessor.Instance?.PendingCount??0},{d.Farm.CommittedThroughSequence},{d.Farm.Beds.Count(x=>x.Value.IsPlanted)},{d.Farm.Beds.Count(x=>x.Value.IsReady)},{Profiler.GetTotalAllocatedMemoryLong()},{Profiler.GetMonoUsedSizeLong()},{Profiler.GetAllocatedMemoryForGraphicsDriver()},{restarts}"));writer.Flush();
     yield return new WaitForSecondsRealtime(1);
    }
    if(d.Forge.AutocraftCrafts<=beforeCraft||d.CauldronTotals.TotalTastings<=beforeTaste)throw new Exception("No real production during "+phase);
   }
  }
  session.StopAutoCrafting();cauldron.StopTasting();windows.CloseAllWindows();mark("stress-recovery");
  yield return new WaitForSecondsRealtime(Mathf.Min(30,phaseSeconds));inventory("stress-final");
  File.WriteAllText(Path.Combine(output,"stress-completion.json"),JsonConvert.SerializeObject(new{mode,elapsed=Time.realtimeSinceStartup-began,craftsCompleted=d.Forge.AutocraftCrafts-initialCrafts,tastingsCompleted=d.CauldronTotals.TotalTastings-initialTastes,farmSequenceDelta=d.Farm.CommittedThroughSequence-initialSequence,restarts,coreBalance=resourceManager.GetAmount(binding.coreResource),ingotBalance=resourceManager.GetAmount(binding.ingotResource),cauldron.Stew,finalInfinity=cauldron.IsInfinityActive()},Formatting.Indented));
  finish();
 }
}
#endif
