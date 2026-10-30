#if UNITY_INCLUDE_TESTS
using System;
using Blindsided.SaveData;
using Blindsided.SaveData.Migrations;
using NUnit.Framework;
using TimelessEchoes.Farming;
using UnityEngine;

namespace Tests.EditMode
{
    public sealed class FarmProjectProgressionTests
    {
        private static readonly DateTime Now = new(2026,10,30,0,0,0,DateTimeKind.Utc);

        [TestCase(19,false,false)]
        [TestCase(20,true,false)]
        [TestCase(49,true,false)]
        [TestCase(50,true,true)]
        public void OldSaveKeepsEarnedAutomationWithoutPaymentOrGrowthReset(int level,bool replant,bool water)
        {
            var old = new GameData { SchemaVersion = 8, LastGameVersion = "1.5.8" };
            old.Farm.ProductionRevision = 1;
            old.Farm.TwinsLevel = level;
            old.Farm.TotalHarvests = 2;
            old.Farm.GardenCapacity = 1;
            old.Farm.Beds[FarmCommands.WestBedId] = new FarmBedState { Unlocked=true,Repeat=true,ElapsedSeconds=123 };
            old.Resources["Log"] = new GameData.ResourceEntry { Amount=987 };
            var migrated=SaveMigrationRunner.TryMigrate(old,"1.5.8");
            Assert.That(migrated.Succeeded,Is.True,migrated.Error);
            Assert.That(FarmProjects.AutoReplant(migrated.Data.Farm),Is.EqualTo(replant));
            Assert.That(FarmProjects.AutoWater(migrated.Data.Farm),Is.EqualTo(water));
            Assert.That(FarmProjects.Complete(migrated.Data.Farm,FarmProject.MillBody),Is.EqualTo(replant));
            Assert.That(migrated.Data.Farm.FirstHarvestCompleted,Is.True);
            Assert.That(migrated.Data.Resources["Log"].Amount,Is.EqualTo(987));
            Assert.That(migrated.Data.Farm.Beds[FarmCommands.WestBedId].ElapsedSeconds,Is.EqualTo(123));
            Assert.That(migrated.Data.Farm.Beds[FarmCommands.WestBedId].Repeat,Is.True);
            Assert.That(old.Farm.Projects,Is.EqualTo(FarmProject.None),"Migration must only mutate its detached candidate.");
            var again=SaveMigrationRunner.TryMigrate(migrated.Data,"1.5.8");
            Assert.That(again.Succeeded,Is.True,again.Error);
            Assert.That(again.Changed,Is.False);
        }

        [Test]
        public void CurrentSchemaDoesNotGainAutomationFromLevelOnReload()
        {
            var current = new GameData { SchemaVersion = GameData.CurrentSchemaVersion, LastGameVersion = "1.5.8" };
            current.Farm.ProductionRevision = 1;
            current.Farm.TwinsLevel = 50;
            var migrated = SaveMigrationRunner.TryMigrate(current, "1.5.8");
            Assert.That(migrated.Succeeded, Is.True, migrated.Error);
            Assert.That(migrated.Data.Farm.Projects, Is.EqualTo(FarmProject.None));
            Assert.That(FarmProjects.AutoReplant(migrated.Data.Farm), Is.False);
            Assert.That(FarmProjects.AutoWater(migrated.Data.Farm), Is.False);
        }

        [Test]
        public void WellAndMillHaveIndependentPaidUnlocksAndSailsCannotSkipTheBody()
        {
            var content=Resources.Load<FarmContent>("Farming/FarmContent");
            var owner=new GameData();owner.CompletedNpcTasks.Add("Farmers1");
            owner.Farm.ProductionRevision=1;
            owner.General.MaxRunDistance=10000;owner.SkillData["Woodcutting"]=new GameData.SkillProgress{Level=100};
            owner.SkillData["Mining"]=new GameData.SkillProgress{Level=100};
            owner.Farm.TwinsLevel=100;owner.Farm.GardenCapacity=1;
            owner.Farm.Beds[FarmCommands.WestBedId]=new FarmBedState{Unlocked=true};
            owner.Quests[FarmContent.IntroductionId]=new GameData.QuestRecord{Completed=true};
            owner.Quests["Farm.Garden.Build01.v1"]=new GameData.QuestRecord{Completed=true};
            foreach(var pair in new[]{("Log",5500d),("Oak Log",2000d),("Oak Stick",4000d),("Feather",5000d),("Stone",50000d),("Radish Seed Pack",2d)})
                owner.Resources[pair.Item1]=new GameData.ResourceEntry{Amount=pair.Item2,Earned=true};
            FarmCommandResult Build(string id)=>FarmCommands.BuildFields(owner,id,FarmJournal.NextOperation(owner.Farm,"build:"+id),Now,content);
            void Apply(FarmCommandResult command)
            {
                Assert.That(command.Accepted,Is.True,command.Reason);
                Assert.That(FarmTransaction.TryPrepare(owner,command,out var next,out var error),Is.True,error);
                owner=next;
            }
            Assert.That(Build(FarmProjects.Well).Accepted,Is.False,"A first harvest is required.");
            owner.Farm.FirstHarvestCompleted=true;
            Assert.That(Build(FarmProjects.MillSails).Accepted,Is.False,"Sails require the finished mill body.");
            Assert.That(FarmCommands.SetRepeat(owner.Farm,FarmCommands.WestBedId,true,FarmJournal.NextOperation(owner.Farm,"repeat:"+FarmCommands.WestBedId+":1"),Now).Accepted,Is.False,"Level alone no longer grants Auto.");
            Apply(FarmCommands.PlantRecipe(owner,FarmCommands.WestBedId,FarmCommands.RadishRecipeId,FarmJournal.NextOperation(owner.Farm,FarmCommands.PlantFingerprint(FarmCommands.WestBedId,FarmCommands.RadishRecipeId)),Now,content));
            owner.Farm.Beds[FarmCommands.WestBedId].ElapsedSeconds=17;
            Apply(Build(FarmProjects.Well));
            Assert.That(owner.Farm.Beds[FarmCommands.WestBedId].Watered,Is.True);
            Assert.That(owner.Farm.Beds[FarmCommands.WestBedId].ElapsedSeconds,Is.EqualTo(17));
            Assert.That(FarmProjects.AutoReplant(owner.Farm),Is.False);
            Assert.That(owner.Resources["Stone"].Amount,Is.Zero);
            Apply(Build(FarmProjects.MillBody));
            Assert.That(FarmProjects.AutoReplant(owner.Farm),Is.False);
            Apply(Build(FarmProjects.MillSails));
            Assert.That(FarmProjects.AutoReplant(owner.Farm),Is.True);
            Assert.That(owner.Farm.GardenCapacity,Is.EqualTo(1),"Buildings must not grant extra plots.");
            Assert.That(owner.Resources["Log"].Amount,Is.Zero);
            Assert.That(owner.Resources["Feather"].Amount,Is.Zero);
            Assert.That(Build(FarmProjects.MillSails).Accepted,Is.False,"A second hand-in must not spend again.");
            Apply(FarmCommands.SetRepeat(owner.Farm,FarmCommands.WestBedId,true,FarmJournal.NextOperation(owner.Farm,"repeat:"+FarmCommands.WestBedId+":1"),Now));
            FarmCommands.TickFields(owner.Farm,100000);
            Apply(FarmCommands.HarvestFields(owner,FarmJournal.NextOperation(owner.Farm,"harvest-auto"),Now,content,true));
            Assert.That(owner.Farm.Beds[FarmCommands.WestBedId].IsPlanted,Is.True);
            Assert.That(owner.Farm.Beds[FarmCommands.WestBedId].Watered,Is.True);
            Assert.That(owner.Resources["Radish Seed Pack"].Amount,Is.Zero,"Automatic replanting still spends a pack.");
        }
    }
}
#endif
