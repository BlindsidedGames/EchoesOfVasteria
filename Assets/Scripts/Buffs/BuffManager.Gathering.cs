using System.Collections.Generic;
using TimelessEchoes.Tasks;
using TimelessEchoes.Upgrades;
using UnityEngine;
using static Blindsided.Oracle;

namespace TimelessEchoes.Buffs
{
    // State belongs to one activation, so expiry, death and returning to town discard it.
    public sealed class GatheringBuffState
    {
        public int completed;
        public readonly Dictionary<Resource, double> bundle = new();
        public readonly Dictionary<int, float> echoCompletions = new();

        public bool AccumulateWindfall(Dictionary<Resource, double> totals)
        {
            if (totals.Count == 0) return false;
            foreach (var pair in totals)
            {
                bundle.TryGetValue(pair.Key, out double current);
                bundle[pair.Key] = current + pair.Value;
            }
            return ++completed >= 10;
        }
    }

    public partial class BuffManager
    {
        private TaskData[] gatheringTasks;
        public event System.Action ProspectorTargetChanged;
        public int ProspectorTaskId => oracle?.saveData?.ProspectorTaskId ?? -1;
        public TaskData ProspectorTarget
        {
            get
            {
                gatheringTasks ??= Resources.LoadAll<TaskData>("Tasks");
                foreach (var task in gatheringTasks)
                    if (task != null && task.taskID == ProspectorTaskId) return task;
                return null;
            }
        }
        public bool HasValidProspectorTarget => ProspectorTarget != null &&
            TaskWeightService.IsTaskUnlocked(ProspectorTarget) && ProspectorTarget.weight > 0;

        public bool SetProspectorTarget(TaskData task)
        {
            if (oracle?.saveData == null || task == null || task.weight <= 0 || !TaskWeightService.IsTaskUnlocked(task)) return false;
            if (TimelessEchoes.Stats.GameplayStatTracker.Instance?.RunInProgress == true) return false;
            oracle.saveData.ProspectorTaskId = task.taskID;
            ProspectorTargetChanged?.Invoke();
            return true;
        }

        private void ResetGatheringForProfileLoad()
        {
            for (int i = activeBuffs.Count - 1; i >= 0; i--)
                if (activeBuffs[i].effects.Exists(e => IsGatheringEffect(e.type))) RemoveBuffAt(i, false);
            var remove = new List<BuffRecipe>();
            foreach (var recipe in cooldowns.Keys)
                if (recipe != null && recipe.baseEffects.Exists(e => IsGatheringEffect(e.type))) remove.Add(recipe);
            foreach (var recipe in remove) cooldowns.Remove(recipe);
        }

        private static bool IsGatheringEffect(BuffEffectType type) => type == BuffEffectType.ProspectorWeightPercent ||
            type == BuffEffectType.CollectorWeightPercent || type == BuffEffectType.WindfallRewardPercent ||
            type == BuffEffectType.EchoResonanceRewardPercent;

        public static float CollectorBonus(float percent, int completions) =>
            Mathf.Clamp(percent, 0f, 200f) / 100f / (1f + Mathf.Max(0, completions) / 100f);

        private static float Effect(ActiveBuff buff, BuffEffectType type)
        {
            foreach (var effect in buff.effects) if (effect.type == type) return effect.value;
            return 0f;
        }

        public float GetGatheringWeightMultiplier(TaskData task)
        {
            if (task == null) return 1f;
            float bonus = 0f;
            foreach (var buff in activeBuffs)
            {
                if (buff.remaining <= 0) continue;
                if (task.taskID == ProspectorTaskId)
                    bonus += Mathf.Clamp(Effect(buff, BuffEffectType.ProspectorWeightPercent), 0f, 200f) / 100f;
                float collector = Effect(buff, BuffEffectType.CollectorWeightPercent);
                if (collector > 0) bonus += CollectorBonus(collector, TaskWeightService.GetTotalCompletions(task));
            }
            // Additive stacking, bounded even with Cauldron power.
            return 1f + Mathf.Min(4f, bonus);
        }

        public double GetResonanceBonus(TaskData task, bool isEcho, double amount)
        {
            if (task == null || isEcho || amount <= 0) return 0;
            double bonus = 0;
            foreach (var buff in activeBuffs)
                if (buff.remaining > 0 && buff.gathering.echoCompletions.TryGetValue(task.taskID, out float when) &&
                    Time.time - when <= 10f)
                    bonus += amount * Mathf.Clamp(Effect(buff, BuffEffectType.EchoResonanceRewardPercent), 0f, 100f) / 100f;
            return bonus;
        }

        public void RecordGatheringCompletion(TaskData task, bool isEcho, Dictionary<Resource, double> totals,
            List<Resource> order, ResourceManager resources)
        {
            if (task == null) return;
            foreach (var buff in activeBuffs)
            {
                if (buff.remaining <= 0) continue;
                if (Effect(buff, BuffEffectType.EchoResonanceRewardPercent) > 0)
                {
                    if (isEcho) buff.gathering.echoCompletions[task.taskID] = Time.time;
                    else buff.gathering.echoCompletions.Remove(task.taskID);
                }
                float windfall = Mathf.Clamp(Effect(buff, BuffEffectType.WindfallRewardPercent), 0f, 100f);
                if (windfall <= 0 || totals.Count == 0) continue;
                if (!buff.gathering.AccumulateWindfall(totals)) continue;
                foreach (var pair in buff.gathering.bundle)
                {
                    double reward = pair.Value * windfall / 100d;
                    resources.Add(pair.Key, reward, bonus: true, eligibleForTierRoll: false);
                    if (!totals.ContainsKey(pair.Key)) { totals[pair.Key] = 0; order.Add(pair.Key); }
                    totals[pair.Key] += reward;
                }
                buff.gathering.bundle.Clear();
                buff.gathering.completed = 0;
            }
        }
    }
}
