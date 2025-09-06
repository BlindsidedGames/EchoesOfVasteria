#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Blindsided.SaveData;
using System.Collections.Generic;
using TimelessEchoes.Editor.Util;

namespace TimelessEchoes.Editor
{
    public class SaveEditorWindow : OdinEditorWindow
    {
        private const string ExportPrefix = "TE1:";

        [MenuItem("Tools/Save Editor")] 
        private static void Open()
        {
            GetWindow<SaveEditorWindow>(utility: false, title: "Save Editor");
        }

        [ShowInInspector]
        [HideReferenceObjectPicker]
        [Title("Game Data", subtitle: "Edit any fields, then Copy/Paste TE1 string" , bold: true)]
        [PropertyOrder(0)]
        public GameData Data = new GameData();

        [ShowInInspector]
        [PropertyOrder(1)]
        [LabelText("TE1 Save String")]
        [MultiLineProperty(10)]
        public string SaveString;

        [ShowInInspector]
        [PropertyOrder(1)]
        [LabelText("JSON (GameData)")]
        [MultiLineProperty(10)]
        public string JsonString;

        [PropertyOrder(2)]
        [HorizontalGroup("Clipboard", 0.5f, LabelWidth = 1)]
        [Button(Icon = SdfIconType.Clipboard, ButtonHeight = 26, Name = "Copy from Data")] 
        private void CopyFromData()
        {
            try
            {
                var te1 = EncodeToTe1(Data);
                SaveString = te1;
                EditorGUIUtility.systemCopyBuffer = te1;
                ShowNotificationSafe("Copied TE1 string to clipboard.");
            }
            catch (Exception ex)
            {
                ShowNotificationSafe($"Copy failed: {ex.Message}");
            }
        }

        [PropertyOrder(2)]
        [HorizontalGroup("Clipboard")]
        [Button(Icon = SdfIconType.ClipboardCheck, ButtonHeight = 26, Name = "Paste to Data")]
        private void PasteToData()
        {
            try
            {
                var clip = EditorGUIUtility.systemCopyBuffer;
                if (string.IsNullOrWhiteSpace(clip))
                {
                    ShowNotificationSafe("Clipboard empty.");
                    return;
                }
                var parsed = DecodeFromTe1(clip);
                Data = parsed;
                SaveString = clip;
                ShowNotificationSafe("Pasted from clipboard into Data.");
            }
            catch (Exception ex)
            {
                ShowNotificationSafe($"Paste failed: {ex.Message}");
            }
        }

        [PropertyOrder(3)]
        [HorizontalGroup("Field", 0.5f, LabelWidth = 1)]
        [Button(Icon = SdfIconType.ArrowDownCircle, ButtonHeight = 24, Name = "Encode to Field")] 
        private void EncodeToField()
        {
            try
            {
                SaveString = EncodeToTe1(Data);
                ShowNotificationSafe("Encoded Data → TE1 field.");
            }
            catch (Exception ex)
            {
                ShowNotificationSafe($"Encode failed: {ex.Message}");
            }
        }

        [PropertyOrder(3)]
        [HorizontalGroup("Field")]
        [Button(Icon = SdfIconType.ArrowDownCircle, ButtonHeight = 24, Name = "JSON → TE1 Field")]
        private void EncodeJsonToField()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(JsonString))
                {
                    ShowNotificationSafe("JSON field is empty.");
                    return;
                }

                // Prefer legacy importer first; if it fails, fall back to Odin JSON
                if (!TryParseLegacyJsonToGameData(JsonString, out var parsed))
                {
                    try
                    {
                        parsed = Sirenix.Serialization.SerializationUtility.DeserializeValue<GameData>(
                            Encoding.UTF8.GetBytes(JsonString),
                            Sirenix.Serialization.DataFormat.JSON);
                    }
                    catch
                    {
                        parsed = null;
                    }
                }

                if (parsed == null)
                {
                    ShowNotificationSafe("Failed to parse JSON into GameData.");
                    return;
                }

                Data = parsed;
                SaveString = EncodeToTe1(Data);
                ShowNotificationSafe("Parsed JSON and updated TE1 field.");
            }
            catch (Exception ex)
            {
                ShowNotificationSafe($"JSON→TE1 failed: {ex.Message}");
            }
        }

        [PropertyOrder(3)]
        [HorizontalGroup("Field")]
        [Button(Icon = SdfIconType.ArrowDownCircle, ButtonHeight = 24, Name = "Data → JSON Field")]
        private void EncodeDataToJsonField()
        {
            try
            {
                byte[] jsonBytes = Sirenix.Serialization.SerializationUtility.SerializeValue(
                    Data,
                    Sirenix.Serialization.DataFormat.JSON);
                JsonString = Encoding.UTF8.GetString(jsonBytes);
                ShowNotificationSafe("Encoded Data → JSON field.");
            }
            catch (Exception ex)
            {
                ShowNotificationSafe($"Data→JSON failed: {ex.Message}");
            }
        }

        [PropertyOrder(3)]
        [HorizontalGroup("Field")]
        [Button(Icon = SdfIconType.ArrowDownCircle, ButtonHeight = 24, Name = "TE1 → JSON Field")]
        private void DecodeTe1ToJsonField()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SaveString))
                {
                    ShowNotificationSafe("TE1 field is empty.");
                    return;
                }
                var data = DecodeFromTe1(SaveString);
                byte[] jsonBytes = Sirenix.Serialization.SerializationUtility.SerializeValue(
                    data,
                    Sirenix.Serialization.DataFormat.JSON);
                JsonString = Encoding.UTF8.GetString(jsonBytes);
                ShowNotificationSafe("Decoded TE1 → JSON field.");
            }
            catch (Exception ex)
            {
                ShowNotificationSafe($"TE1→JSON failed: {ex.Message}");
            }
        }

        [PropertyOrder(3)]
        [HorizontalGroup("Field")]
        [Button(Icon = SdfIconType.ArrowUpCircle, ButtonHeight = 24, Name = "Decode to Data")]
        private void DecodeToData()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SaveString))
                {
                    ShowNotificationSafe("TE1 field is empty.");
                    return;
                }
                Data = DecodeFromTe1(SaveString);
                ShowNotificationSafe("Decoded TE1 field → Data.");
            }
            catch (Exception ex)
            {
                ShowNotificationSafe($"Decode failed: {ex.Message}");
            }
        }

        [PropertyOrder(4)]
        [Title("Slots (Optional)", subtitle: "Independent of runtime. Reads/writes slot files.")]
        [EnumToggleButtons]
        public SlotOption Slot = SlotOption.Save1;

        public enum SlotOption { Save1, Save2, Save3 }

        [PropertyOrder(5)]
        [HorizontalGroup("Slots", 0.5f, LabelWidth = 1)]
        [Button(Icon = SdfIconType.Download, ButtonHeight = 24, Name = "Load Slot → Data")]
        private void LoadFromSlot()
        {
            try
            {
                var slotName = SlotToName(Slot);
                SaveManager.Instance.SetCurrentSlot(slotName);
                var (ok, data) = SaveManager.Instance.LoadAsync().GetAwaiter().GetResult();
                if (!ok || data == null)
                {
                    ShowNotificationSafe($"No valid snapshot in {slotName}.");
                    return;
                }
                Data = data;
                ShowNotificationSafe($"Loaded {slotName} into Data.");
            }
            catch (Exception ex)
            {
                ShowNotificationSafe($"Load failed: {ex.Message}");
            }
        }

        [PropertyOrder(5)]
        [HorizontalGroup("Slots")]
        [Button(Icon = SdfIconType.Upload, ButtonHeight = 24, Name = "Save Data → Slot")]
        private void SaveToSlot()
        {
            try
            {
                var slotName = SlotToName(Slot);
                SaveManager.Instance.SetCurrentSlot(slotName);
                var ok = SaveManager.Instance.SaveAsync(Data).GetAwaiter().GetResult();
                ShowNotificationSafe(ok ? $"Saved Data to {slotName}." : $"Save to {slotName} failed.");
            }
            catch (Exception ex)
            {
                ShowNotificationSafe($"Save failed: {ex.Message}");
            }
        }

        private static string SlotToName(SlotOption slot) => slot switch
        {
            SlotOption.Save1 => "Save1",
            SlotOption.Save2 => "Save2",
            SlotOption.Save3 => "Save3",
            _ => "Save1"
        };

        // --- TE1 helpers (mirror of SaveImportExport without Oracle coupling) ---
        private static string EncodeToTe1(GameData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            byte[] binary = Sirenix.Serialization.SerializationUtility.SerializeValue(data, Sirenix.Serialization.DataFormat.Binary);
            byte[] compressed = Deflate(binary);
            string encoded = Base64UrlEncode(compressed);
            return ExportPrefix + encoded;
        }

        private static GameData DecodeFromTe1(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) throw new ArgumentException("Empty input", nameof(input));
            if (!input.StartsWith(ExportPrefix, StringComparison.Ordinal)) throw new FormatException("Invalid prefix (expected TE1:)");
            if (input.Length > 1_000_000) throw new FormatException("Input too long");

            string payload = input.Substring(ExportPrefix.Length);
            byte[] compressed = Base64UrlDecode(payload);
            byte[] binary = Inflate(compressed);
            var data = Sirenix.Serialization.SerializationUtility.DeserializeValue<GameData>(binary, Sirenix.Serialization.DataFormat.Binary);
            if (data == null) throw new Exception("Failed to decode save");
            return data;
        }

        private static byte[] Deflate(byte[] input)
        {
            using (var output = new MemoryStream())
            {
                using (var ds = new System.IO.Compression.DeflateStream(output, System.IO.Compression.CompressionLevel.Optimal, true))
                {
                    ds.Write(input, 0, input.Length);
                }
                return output.ToArray();
            }
        }

        private static byte[] Inflate(byte[] input)
        {
            using (var ms = new MemoryStream(input))
            using (var ds = new System.IO.Compression.DeflateStream(ms, System.IO.Compression.CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                ds.CopyTo(output);
                return output.ToArray();
            }
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            string s = Convert.ToBase64String(bytes);
            s = s.Replace('+', '-').Replace('/', '_');
            return s.TrimEnd('=');
        }

        private static byte[] Base64UrlDecode(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/');
            s = s.PadRight((s.Length + 3) / 4 * 4, '=');
            return Convert.FromBase64String(s);
        }

        // --- Legacy JSON (Beta5 style) importer ---
        private static bool TryParseLegacyJsonToGameData(string input, out GameData result)
        {
            result = null;
            try
            {
                var rootObj = MiniJson.Deserialize(input) as Dictionary<string, object>;
                if (rootObj == null) return false;

                // Unwrap { anyKey: { __type: ..., value: { ... } } } or { __type:..., value:{...} }
                Dictionary<string, object> valObj = null;
                if (rootObj.ContainsKey("value"))
                {
                    valObj = rootObj["value"] as Dictionary<string, object>;
                }
                else
                {
                    foreach (var kv in rootObj)
                    {
                        if (kv.Value is Dictionary<string, object> inner && inner.ContainsKey("value"))
                        {
                            valObj = inner["value"] as Dictionary<string, object>;
                            break;
                        }
                    }
                }
                if (valObj == null) return false;

                var gd = new GameData();

                // Preferences
                if (valObj.TryGetValue("SavedPreferences", out var prefsObj) && prefsObj is Dictionary<string, object> prefs)
                {
                    TryGetInt(prefs, "BuyMode", out int buyMode);
                    if (buyMode >= 0) gd.SavedPreferences.BuyMode = (GameData.BuyMode)Math.Min((int)GameData.BuyMode.BuyMax, buyMode);
                    TryGetBool(prefs, "ExtraBuyOptions", out gd.SavedPreferences.ExtraBuyOptions);
                    TryGetBool(prefs, "InvertMenu", out gd.SavedPreferences.InvertMenu);
                    TryGetInt(prefs, "LayerTab", out int layerTab);
                    if (layerTab >= 0) gd.SavedPreferences.LayerTab = (GameData.Tab)Math.Min((int)GameData.Tab.ChronicleArchives, layerTab);
                    TryGetBool(prefs, "Music", out gd.SavedPreferences.Music);
                    TryGetInt(prefs, "Notation", out int notation);
                    if (notation >= 0) gd.SavedPreferences.Notation = (GameData.NumberTypes)Math.Min((int)GameData.NumberTypes.Engineering, notation);
                    TryGetBool(prefs, "OfflineTimeActive", out gd.SavedPreferences.OfflineTimeActive);
                    TryGetBool(prefs, "OfflineTimeAutoDisable", out gd.SavedPreferences.OfflineTimeAutoDisable);
                    TryGetBool(prefs, "RoundedBulkBuy", out gd.SavedPreferences.RoundedBulkBuy);
                    TryGetBool(prefs, "SettingsFoldout", out gd.SavedPreferences.SettingsFoldout);
                    TryGetBool(prefs, "ShopFoldout", out gd.SavedPreferences.ShopFoldout);
                    TryGetBool(prefs, "ShortLongCurrencyDisplay", out gd.SavedPreferences.ShortLongCurrencyDisplay);
                    TryGetBool(prefs, "ShowLevelText", out gd.SavedPreferences.ShowLevelText);
                    TryGetBool(prefs, "StatsFoldout", out gd.SavedPreferences.StatsFoldout);
                    TryGetBool(prefs, "TransparentUi", out gd.SavedPreferences.TransparentUi);
                    TryGetBool(prefs, "Tutorial", out gd.SavedPreferences.Tutorial);
                    TryGetBool(prefs, "ShowPinnedQuests", out gd.SavedPreferences.ShowPinnedQuests);
                    TryGetBool(prefs, "UseScaledTimeForValues", out gd.SavedPreferences.UseScaledTimeForValues);
                }

                // Time fields
                TryGetFloat(valObj, "CurrentTime", out gd.CurrentTime);
                TryGetString(valObj, "DateQuitString", out gd.DateQuitString);
                TryGetString(valObj, "DateStarted", out gd.DateStarted);
                TryGetDouble(valObj, "OfflineTime", out gd.OfflineTime);
                TryGetDouble(valObj, "OfflineTimeCap", out gd.OfflineTimeCap);
                TryGetDouble(valObj, "OfflineTimeScaleMultiplier", out gd.OfflineTimeScaleMultiplier);
                TryGetDouble(valObj, "PlayTime", out gd.PlayTime);
                TryGetFloat(valObj, "TimeScale", out gd.TimeScale);

                // SkillData
                if (valObj.TryGetValue("SkillData", out var skillsObj) && skillsObj is Dictionary<string, object> skills)
                {
                    foreach (var sk in skills)
                    {
                        if (sk.Value is Dictionary<string, object> sd)
                        {
                            var sp = new GameData.SkillProgress();
                            if (TryGetDouble(sd, "CurrentXP", out var cxp)) sp.CurrentXP = (float)cxp;
                            TryGetInt(sd, "Level", out sp.Level);
                            if (sd.TryGetValue("Milestones", out var milestonesObj) && milestonesObj is List<object> milestones)
                            {
                                foreach (var m in milestones) if (m is string sm) sp.Milestones.Add(sm);
                            }
                            gd.SkillData[sk.Key] = sp;
                        }
                    }
                }

                // Upgrades
                if (valObj.TryGetValue("UpgradeLevels", out var upgradesObj) && upgradesObj is Dictionary<string, object> upgrades)
                {
                    foreach (var kv in upgrades)
                    {
                        if (TryToInt(kv.Value, out var ival)) gd.UpgradeLevels[kv.Key] = ival;
                    }
                }

                // Resources
                if (valObj.TryGetValue("Resources", out var resObj) && resObj is Dictionary<string, object> res)
                {
                    foreach (var kv in res)
                    {
                        if (kv.Value is Dictionary<string, object> re)
                        {
                            var entry = new GameData.ResourceEntry();
                            TryGetDouble(re, "Amount", out entry.Amount);
                            TryGetBool(re, "Earned", out entry.Earned);
                            TryGetDouble(re, "BestPerMinute", out entry.BestPerMinute);
                            if (TryGetInt(re, "Tier", out var tier)) entry.Tier = tier;
                            gd.Resources[kv.Key] = entry;
                        }
                    }
                }

                // EnemyKills
                if (valObj.TryGetValue("EnemyKills", out var ekObj) && ekObj is Dictionary<string, object> ek)
                {
                    foreach (var kv in ek)
                    {
                        if (TryToDouble(kv.Value, out var d)) gd.EnemyKills[kv.Key] = d;
                    }
                }

                // Buffs
                if (valObj.TryGetValue("BuffSlots", out var bsObj) && bsObj is List<object> bs)
                {
                    gd.BuffSlots.Clear();
                    foreach (var b in bs) gd.BuffSlots.Add(b as string);
                }
                TryGetInt(valObj, "UnlockedBuffSlots", out gd.UnlockedBuffSlots);
                TryGetInt(valObj, "UnlockedAutoBuffSlots", out gd.UnlockedAutoBuffSlots);
                if (valObj.TryGetValue("AutoBuffSlots", out var absObj) && absObj is List<object> absl)
                {
                    gd.AutoBuffSlots.Clear();
                    foreach (var b in absl) gd.AutoBuffSlots.Add(b is bool vb && vb);
                }

                // Completed tasks
                if (valObj.TryGetValue("CompletedNpcTasks", out var ctnObj) && ctnObj is List<object> ct)
                {
                    gd.CompletedNpcTasks.Clear();
                    foreach (var t in ct) if (t is string st) gd.CompletedNpcTasks.Add(st);
                }

                // Disciples
                if (valObj.TryGetValue("Disciples", out var discObj) && discObj is Dictionary<string, object> disc)
                {
                    foreach (var kv in disc)
                    {
                        if (kv.Value is Dictionary<string, object> d)
                        {
                            var rec = new GameData.DiscipleGenerationRecord();
                            if (d.TryGetValue("StoredResources", out var srObj) && srObj is Dictionary<string, object> srd)
                                foreach (var r in srd) if (TryToDouble(r.Value, out var dv)) rec.StoredResources[r.Key] = dv;
                            if (d.TryGetValue("TotalCollected", out var tcObj) && tcObj is Dictionary<string, object> tcd)
                                foreach (var r in tcd) if (TryToDouble(r.Value, out var dv)) rec.TotalCollected[r.Key] = dv;
                            TryGetFloat(d, "Progress", out rec.Progress);
                            TryGetDouble(d, "LastGenerationTime", out rec.LastGenerationTime);
                            gd.Disciples[kv.Key] = rec;
                        }
                    }
                }

                // Quests
                if (valObj.TryGetValue("Quests", out var questsObj) && questsObj is Dictionary<string, object> qd)
                {
                    foreach (var kv in qd)
                    {
                        if (kv.Value is Dictionary<string, object> q)
                        {
                            var qr = new GameData.QuestRecord();
                            TryGetBool(q, "Completed", out qr.Completed);
                            if (q.TryGetValue("KillProgress", out var kpObj) && kpObj is Dictionary<string, object> kpd)
                            {
                                qr.KillProgress = new Dictionary<string, double>();
                                foreach (var e in kpd) if (TryToDouble(e.Value, out var dv)) qr.KillProgress[e.Key] = dv;
                            }
                            TryGetInt(q, "BuffCastBaseline", out qr.BuffCastBaseline);
                            if (q.TryGetValue("BuffCastBaselineSet", out var bcsVal) && bcsVal is bool bcs) qr.BuffCastBaselineSet = bcs;
                            gd.Quests[kv.Key] = qr;
                        }
                    }
                }

                // Pinned quests
                if (valObj.TryGetValue("PinnedQuests", out var pqObj) && pqObj is List<object> pql)
                {
                    gd.PinnedQuests.Clear();
                    foreach (var p in pql) if (p is string sp) gd.PinnedQuests.Add(sp);
                }

                // TaskRecords
                if (valObj.TryGetValue("TaskRecords", out var trObj) && trObj is Dictionary<string, object> trd)
                {
                    foreach (var kv in trd)
                    {
                        if (kv.Value is Dictionary<string, object> tr)
                        {
                            var rec = new GameData.TaskRecord();
                            TryGetInt(tr, "TotalCompleted", out rec.TotalCompleted);
                            TryGetFloat(tr, "TimeSpent", out rec.TimeSpent);
                            TryGetFloat(tr, "XpGained", out rec.XpGained);
                            if (int.TryParse(kv.Key, out var key)) gd.TaskRecords[key] = rec;
                        }
                    }
                }

                // ResourceStats
                if (valObj.TryGetValue("ResourceStats", out var rsObj) && rsObj is Dictionary<string, object> rs)
                {
                    foreach (var kv in rs)
                    {
                        if (kv.Value is Dictionary<string, object> rr)
                        {
                            var rec = new GameData.ResourceRecord();
                            TryGetDouble(rr, "TotalReceived", out rec.TotalReceived);
                            TryGetDouble(rr, "TotalSpent", out rec.TotalSpent);
                            gd.ResourceStats[kv.Key] = rec;
                        }
                    }
                }

                // MapStats
                if (valObj.TryGetValue("MapStats", out var mapStatsObj) && mapStatsObj is Dictionary<string, object> mapStats)
                {
                    foreach (var kv in mapStats)
                    {
                        if (kv.Value is Dictionary<string, object> md)
                        {
                            var m = new GameData.MapStatistics();
                            if (TryGetDouble(md, "Steps", out var ssteps)) m.StepsDouble = ssteps;
                            if (TryGetDouble(md, "LongestTrek", out var lt)) m.LongestTrekDouble = lt;
                            TryGetInt(md, "TasksCompleted", out m.TasksCompleted);
                            TryGetDouble(md, "ResourcesGathered", out m.ResourcesGathered);
                            TryGetInt(md, "Kills", out m.Kills);
                            if (TryGetDouble(md, "DamageDealt", out var dd)) m.DamageDealtDouble = dd;
                            TryGetInt(md, "Deaths", out m.Deaths);
                            if (TryGetDouble(md, "DamageTaken", out var dt)) m.DamageTakenDouble = dt;
                            gd.MapStats[kv.Key] = m;
                        }
                    }
                }

                // General
                if (valObj.TryGetValue("General", out var gObj) && gObj is Dictionary<string, object> g)
                {
                    var gen = new GameData.GeneralStats();
                    if (TryGetDouble(g, "DistanceTravelled", out var dist)) gen.DistanceTravelledDouble = dist;
                    TryGetFloat(g, "HighestDistance", out gen.HighestDistance);
                    TryGetInt(g, "TotalKills", out gen.TotalKills);
                    TryGetInt(g, "SlimesKilled", out gen.SlimesKilled);
                    TryGetInt(g, "TasksCompleted", out gen.TasksCompleted);
                    TryGetInt(g, "Deaths", out gen.Deaths);
                    if (TryGetDouble(g, "DamageDealt", out var dd)) gen.DamageDealtDouble = dd;
                    if (TryGetDouble(g, "DamageTaken", out var dt)) gen.DamageTakenDouble = dt;
                    TryGetInt(g, "TimesReaped", out gen.TimesReaped);
                    TryGetInt(g, "BuffsCast", out gen.BuffsCast);
                    TryGetDouble(g, "TotalResourcesGathered", out gen.TotalResourcesGathered);
                    if (g.TryGetValue("RecentRuns", out var rrObj) && rrObj is List<object> runs)
                    {
                        foreach (var ro in runs)
                        {
                            if (ro is Dictionary<string, object> r)
                            {
                                var rec = new GameData.RunRecord();
                                TryGetInt(r, "RunNumber", out rec.RunNumber);
                                TryGetString(r, "MapType", out rec.MapType);
                                TryGetFloat(r, "Duration", out rec.Duration);
                                TryGetFloat(r, "Distance", out rec.Distance);
                                TryGetInt(r, "TasksCompleted", out rec.TasksCompleted);
                                if (TryGetDouble(r, "ResourcesCollected", out var rc)) rec.ResourcesCollected = rc;
                                if (TryGetDouble(r, "BonusResourcesCollected", out var brc)) rec.BonusResourcesCollected = brc;
                                TryGetInt(r, "EnemiesKilled", out rec.EnemiesKilled);
                                if (TryGetDouble(r, "DamageDealt", out var rdd)) rec.DamageDealtDouble = rdd;
                                if (TryGetDouble(r, "DamageTaken", out var rdt)) rec.DamageTakenDouble = rdt;
                                TryGetBool(r, "Died", out rec.Died);
                                TryGetBool(r, "Reaped", out rec.Reaped);
                                TryGetBool(r, "Abandoned", out rec.Abandoned);
                                gen.RecentRuns.Add(rec);
                            }
                        }
                    }
                    TryGetFloat(g, "LongestRun", out gen.LongestRun);
                    TryGetFloat(g, "ShortestRun", out gen.ShortestRun);
                    TryGetFloat(g, "AverageRun", out gen.AverageRun);
                    TryGetFloat(g, "MaxRunDistance", out gen.MaxRunDistance);
                    TryGetInt(g, "NextRunNumber", out gen.NextRunNumber);
                    gd.General = gen;
                }

                // Cauldron basics
                TryGetDouble(valObj, "CauldronStew", out gd.CauldronStew);
                TryGetInt(valObj, "CauldronEvaLevel", out gd.CauldronEvaLevel);
                TryGetDouble(valObj, "CauldronEvaXp", out gd.CauldronEvaXp);
                if (valObj.TryGetValue("CauldronCardCounts", out var cccObj) && cccObj is Dictionary<string, object> cccd)
                {
                    foreach (var kv in cccd) if (TryToInt(kv.Value, out var iv)) gd.CauldronCardCounts[kv.Key] = iv;
                }
                TryGetBool(valObj, "CauldronShowAllCards", out gd.CauldronShowAllCards);

                result = gd;
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        private static bool TryGetString(Dictionary<string, object> d, string k, out string v)
        { v = null; if (d.TryGetValue(k, out var o) && o is string s) { v = s; return true; } return false; }
        private static bool TryGetBool(Dictionary<string, object> d, string k, out bool v)
        { v = false; if (d.TryGetValue(k, out var o) && o is bool b) { v = b; return true; } return false; }
        private static bool TryGetInt(Dictionary<string, object> d, string k, out int v)
        { v = 0; if (d.TryGetValue(k, out var o) && TryToInt(o, out var i)) { v = i; return true; } return false; }
        private static bool TryGetFloat(Dictionary<string, object> d, string k, out float v)
        { v = 0f; if (d.TryGetValue(k, out var o) && TryToDouble(o, out var dd)) { v = (float)dd; return true; } return false; }
        private static bool TryGetDouble(Dictionary<string, object> d, string k, out double v)
        { v = 0d; if (d.TryGetValue(k, out var o) && TryToDouble(o, out var dv)) { v = dv; return true; } return false; }

        private static bool TryToDouble(object o, out double d)
        { if (o is double dd) { d = dd; return true; } if (o is long l) { d = l; return true; } if (o is int i) { d = i; return true; } d = 0; return false; }
        private static bool TryToInt(object o, out int i)
        { if (o is int ii) { i = ii; return true; } if (o is long l) { i = (int)l; return true; } if (o is double d) { i = (int)d; return true; } i = 0; return false; }

        private void ShowNotificationSafe(string message)
        {
            try
            {
                this.ShowNotification(new GUIContent(message));
            }
            catch
            {
                Debug.Log(message);
            }
        }
    }
}
#endif


