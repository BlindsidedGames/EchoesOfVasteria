using System;
using System.Collections.Generic;
using Blindsided.SaveData;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Quests;
using TimelessEchoes.UI.Cauldron;

namespace Blindsided
{
    public partial class Oracle
    {
        public bool TryCommitCauldronConversion(Resource food, double quantity, double expectedUnitValue,
            long sequence, out double stewGained, out string error)
        {
            stewGained = 0; error = null;
            if (!CanBeginSaveMutation || !CanSave()) { error = "Save is busy or requires recovery."; return false; }
            if (food == null || !CauldronMixingPresentation.IsFood(food) ||
                CauldronConversion.UnitValue(food) != expectedUnitValue)
            { error = "The food value changed. Select it again."; return false; }
            _economicTransactionActive = true;
            try
            {
                StopSaveCoordinatorMonitoring(); CompleteActiveSaveBlocking(); _savePending = false;
                if (!TryPrepareSnapshot(out error)) return false;
                var owner = saveData; var slot = CurrentSlot;
                var questIds = QuestManager.Instance?.GetActiveCauldronQuestIds() ?? new List<string>();
                if (!CauldronConversion.TryPrepare(owner, food.name, quantity, expectedUnitValue, sequence,
                    questIds, out var candidate, out var replay, out error)) return false;
                if (replay) return true;
                var result = GetSaveResult(SaveManager.Instance.SaveDetailedAsync(candidate,
                    GetSlotDirectoryName(slot)), slot, waitForCompletion: true);
                HandleSaveResult(result, slot);
                if (!result.Succeeded) { error = result.Error; return false; }
                if (!ReferenceEquals(owner, saveData) || CurrentSlot != slot)
                { RequireRuntimeReloadRecovery("The food conversion committed", new InvalidOperationException("Save ownership changed before publication")); error = "Committed conversion requires reload."; return false; }
                try
                {
                    owner.Resources[food.name] = candidate.Resources[food.name];
                    owner.ResourceStats[food.name] = candidate.ResourceStats[food.name];
                    owner.CauldronStew = candidate.CauldronStew;
                    owner.CauldronConversionSequence = candidate.CauldronConversionSequence;
                    owner.LastCauldronConversion = candidate.LastCauldronConversion;
                    foreach (var id in questIds)
                        if (candidate.Quests.TryGetValue(id, out var quest)) owner.Quests[id] = quest;
                    ResourceManager.Instance?.PublishCommittedResource(food.name, owner.Resources[food.name], owner.ResourceStats[food.name]);
                    CauldronManager.Instance?.PublishCommittedConversion(quantity);
                    stewGained = quantity * expectedUnitValue;
                    return true;
                }
                catch (Exception ex)
                {
                    RequireRuntimeReloadRecovery("The food conversion committed", ex);
                    error = "Conversion committed; reload is required: " + ex.Message; return false;
                }
            }
            catch (Exception ex) { error = ex.Message; return false; }
            finally
            {
                _economicTransactionActive = false;
                if (_savePending && CanSave()) { _savePending = false; RequestSave(); }
            }
        }
    }
}
