#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Blindsided.SaveData;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace TimelessEchoes.EditorTools
{
	/// <summary>
	/// Editor window to browse, load, inspect, and edit ES3 save files.
	/// - Slot mode: auto-composes file/key names used by Oracle (BetaX Sd{slot}.es3 / Data{slot})
	/// - File mode: open any .es3 file and specify a key to edit
	/// Provides a rich inspector for the "GameData" object and a simple viewer for other keys.
	/// </summary>
	public class ES3SaveEditorWindow : OdinEditorWindow
	{
		[MenuItem("Tools/Save/ES3 Save Editor")] private static void Open()
		{
			var wnd = GetWindow<ES3SaveEditorWindow>(utility: false, title: "ES3 Save Editor", focus: true);
			wnd.minSize = new Vector2(980, 640);
			wnd.Show();
		}

        // Simplified: always operate in file mode

		// Slot-based loading (matches Oracle naming)
		[BoxGroup("Source"), LabelText("Use Beta Prefix")] private bool beta;
		[BoxGroup("Source"), ShowIf("beta"), LabelText("Beta Iteration"), MinValue(0)] private int betaSaveIteration;
		[BoxGroup("Source"), LabelText("Slot"), MinValue(0), MaxValue(2)] private int slot = 0;

		// File-based loading
        [BoxGroup("Source"), LabelText(".es3 File (absolute)"), ReadOnly]
        [SerializeField] private string absoluteFilePath = string.Empty;

		// Loaded state
		private ES3Settings loadedSettings;
		private ES3File loadedFile;
		private string[] keys = Array.Empty<string>();
		private string selectedKey = string.Empty;
		private Vector2 rightScroll;
        
		private string rawJsonCache = string.Empty;
        private bool showRawJson;
        private bool rawEditable;
        private string rawJsonEdited = string.Empty;
        private bool autoSaveOnEdit;

		// GameData editing
		[ShowInInspector, LabelText("GameData"), PropertyOrder(100)]
		private GameData gameData;
        private string gameDataKeyLoaded;
		private Sirenix.OdinInspector.Editor.PropertyTree gameDataTree;
		private bool gameDataDirty;

		// Arbitrary key value preview/edit
		private object selectedValue;
		private Sirenix.OdinInspector.Editor.PropertyTree selectedValueTree;
		private bool selectedValueDirty;

		protected override void OnEnable()
		{
			base.OnEnable();
			TryAutoDetectFromOracle();
		}

		protected override void OnImGUI()
		{
			DrawToolbar();
			EditorGUILayout.Space(4);
			DrawSourceBox();
			EditorGUILayout.Space(6);
			DrawMainArea();
		}

		private void DrawToolbar()
		{
			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
			{
                GUILayout.Label("ES3 Save Editor", EditorStyles.boldLabel, GUILayout.Width(160));
                GUILayout.FlexibleSpace();
                autoSaveOnEdit = GUILayout.Toggle(autoSaveOnEdit, "Auto Save", EditorStyles.toolbarButton, GUILayout.Width(90));
                showRawJson = GUILayout.Toggle(showRawJson, "Raw JSON", EditorStyles.toolbarButton, GUILayout.Width(90));
				if (GUILayout.Button("Open Save Folder", EditorStyles.toolbarButton, GUILayout.Width(140))) OpenSaveFolder();
				if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(80))) Reload();
			}
		}

		private void DrawSourceBox()
		{
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField(".es3 File (absolute)", absoluteFilePath);
            if (GUILayout.Button("Browse...", GUILayout.Width(100))) BrowseForFile();
            EditorGUILayout.EndHorizontal();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Load File", GUILayout.Width(140), GUILayout.Height(24))) LoadAbsolute();
            }
			EditorGUILayout.EndVertical();
		}

		private void DrawMainArea()
		{
			if (loadedFile == null)
			{
				EditorGUILayout.HelpBox("No file loaded. Load a slot or choose a file.", MessageType.Info);
				return;
			}

            if (showRawJson)
                DrawRawPane();
            else
                DrawInspectorPane();
		}

        // Keys pane removed for simplified single-key workflow

        private void DrawRawPane()
		{
			EditorGUILayout.BeginVertical();
			GUILayout.Label("Raw File JSON", EditorStyles.boldLabel);
			EditorGUILayout.BeginVertical("box");
			rightScroll = EditorGUILayout.BeginScrollView(rightScroll);
            if (!rawEditable)
            {
                if (string.IsNullOrEmpty(rawJsonCache))
                    GUILayout.Label("(empty)", EditorStyles.miniLabel);
                else
                    EditorGUILayout.TextArea(rawJsonCache, GUILayout.ExpandHeight(true));
            }
            else
            {
                rawJsonEdited = EditorGUILayout.TextArea(string.IsNullOrEmpty(rawJsonEdited) ? rawJsonCache : rawJsonEdited, GUILayout.ExpandHeight(true));
            }
			EditorGUILayout.EndScrollView();
			EditorGUILayout.EndVertical();

			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();
                rawEditable = GUILayout.Toggle(rawEditable, rawEditable ? "Editable" : "Read-only", EditorStyles.miniButtonLeft, GUILayout.Width(90));
                if (GUILayout.Button("Apply Raw", EditorStyles.miniButtonMid, GUILayout.Width(100)))
                {
                    ApplyRawJson(autoSaveOnEdit);
                }
                if (GUILayout.Button("Copy All", EditorStyles.miniButtonRight, GUILayout.Width(100)))
                    EditorGUIUtility.systemCopyBuffer = rawJsonCache ?? string.Empty;
			}

			EditorGUILayout.EndVertical();
		}

		private void DrawInspectorPane()
		{
			EditorGUILayout.BeginVertical();
			GUILayout.Label("Inspector", EditorStyles.boldLabel);
			EditorGUILayout.BeginVertical("box");
			rightScroll = EditorGUILayout.BeginScrollView(rightScroll);

			if (gameData != null)
			{
				EditorGUILayout.LabelField("GameData (rich editor)", EditorStyles.boldLabel);
				if (gameDataTree == null)
				{
					gameDataTree = Sirenix.OdinInspector.Editor.PropertyTree.Create(gameData);
					gameDataTree.UpdateTree();
				}
				EditorGUI.BeginChangeCheck();
                gameDataTree.Draw(false);
                if (EditorGUI.EndChangeCheck())
                {
                    gameDataDirty = true;
                    if (autoSaveOnEdit) CommitChanges();
                }
			}
			else if (selectedValue != null)
			{
				EditorGUILayout.LabelField($"Key: {selectedKey}", EditorStyles.boldLabel);
				if (selectedValueTree == null)
				{
					selectedValueTree = Sirenix.OdinInspector.Editor.PropertyTree.Create(selectedValue);
					selectedValueTree.UpdateTree();
				}
                EditorGUI.BeginChangeCheck();
                selectedValueTree.Draw(selectedValue is UnityEngine.Object);
                if (EditorGUI.EndChangeCheck())
                {
                    selectedValueDirty = true;
                    if (autoSaveOnEdit) CommitChanges();
                }
			}
			else
			{
				EditorGUILayout.HelpBox("Select a key to preview/edit.", MessageType.None);
			}

			EditorGUILayout.EndScrollView();
			EditorGUILayout.EndVertical();

			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();
				GUI.enabled = gameDataDirty || selectedValueDirty;
				if (GUILayout.Button("Save Changes", GUILayout.Width(140), GUILayout.Height(26))) CommitChanges();
				GUI.enabled = true;
			}

			EditorGUILayout.EndVertical();
		}

		private void TryAutoDetectFromOracle()
		{
			var oracleType = Type.GetType("Blindsided.Oracle, Assembly-CSharp");
			if (oracleType != null)
			{
				// Attempt to read PlayerPrefs for current slot if present
				try
				{
					var slot = Mathf.Clamp(PlayerPrefs.GetInt("SaveSlot", 0), 0, 2);
					this.slot = slot;
				}
				catch { }
			}
		}

		private void BrowseForFile()
		{
			var dir = GetSaveFolderPath();
			var path = EditorUtility.OpenFilePanel("Open ES3 File", dir, "es3");
			if (!string.IsNullOrEmpty(path))
			{
				absoluteFilePath = path;
			}
		}

        // Slot mode removed

		private void LoadAbsolute()
		{
			if (string.IsNullOrEmpty(absoluteFilePath))
			{
				ShowNotification(new GUIContent("Choose a .es3 file first"));
				return;
			}
            loadedSettings = new ES3Settings(absoluteFilePath, ES3.Location.File);
            LoadFromSettings(loadedSettings, string.Empty, preferGameData: true);
		}

		private void LoadFromSettings(ES3Settings settings, string primaryKey, bool preferGameData)
		{
			try
			{
                // Always operate on the persisted file when enumerating keys
                var fileSettings = new ES3Settings(settings.path, ES3.Location.File);
                try { ES3.CacheFile(fileSettings); } catch { }
                loadedFile = new ES3File(fileSettings);
                try { keys = ES3.GetKeys(fileSettings) ?? Array.Empty<string>(); }
                catch { keys = loadedFile.GetKeys() ?? Array.Empty<string>(); }
				selectedKey = string.Empty;
				selectedValue = null;
				selectedValueTree = null;
				selectedValueDirty = false;
				gameData = null;
				gameDataTree = null;
				gameDataDirty = false;
                rawJsonCache = loadedFile.LoadRawString();
                rawJsonEdited = string.Empty;

                if (preferGameData)
                {
                    try
                    {
                        var keyToUse = !string.IsNullOrEmpty(primaryKey) && (keys?.Contains(primaryKey) ?? false)
                            ? primaryKey
                            : FindPreferredKey(keys);
                        if (!string.IsNullOrEmpty(keyToUse))
                        {
                            gameData = ES3.Load<GameData>(keyToUse, fileSettings);
                            EnsureGameDataDefaults(gameData);
                            gameDataTree = Sirenix.OdinInspector.Editor.PropertyTree.Create(gameData);
                            gameDataTree.UpdateTree();
                            gameDataKeyLoaded = keyToUse;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Failed to load GameData: {ex.Message}");
                    }
                }

				Repaint();
			}
			catch (Exception ex)
			{
				Debug.LogError($"Failed to load ES3 file: {ex}");
				ShowNotification(new GUIContent("Load failed. See console."));
			}
		}

        private ES3Settings ResolveEffectiveSettings(ES3Settings baseSettings) => new ES3Settings(baseSettings.path, ES3.Location.File);

        // Key selection removed in simplified mode

		private void EnsureGameDataDefaults(GameData data)
		{
			if (data == null) return;
			// Mirror Oracle.NullCheckers to ensure all collections exist so Odin can render tabs/sections
            data.SavedPreferences ??= new GameData.Preferences();
			data.Resources ??= new Dictionary<string, GameData.ResourceEntry>();
			data.SkillData ??= new Dictionary<string, GameData.SkillProgress>();
			data.EnemyKills ??= new Dictionary<string, double>();
			data.CompletedNpcTasks ??= new HashSet<string>();
			data.PinnedQuests ??= new List<string>();
			data.EquipmentBySlot ??= new Dictionary<string, GearItemRecord>();
			data.CraftHistory ??= new List<GearItemRecord>();
            data.UpgradeLevels ??= new Dictionary<string, int>();
            data.TaskRecords ??= new Dictionary<int, GameData.TaskRecord>();
            data.ResourceStats ??= new Dictionary<string, GameData.ResourceRecord>();
            data.MapStats ??= new Dictionary<string, GameData.MapStatistics>();
            data.Disciples ??= new Dictionary<string, GameData.DiscipleGenerationRecord>();
			data.BuffSlots ??= new List<string>(new string[5]);
			if (data.BuffSlots.Count < 5)
				while (data.BuffSlots.Count < 5)
					data.BuffSlots.Add(null);
			data.AutoBuffSlots ??= new List<bool>(new bool[5]);
			if (data.AutoBuffSlots.Count < 5)
				while (data.AutoBuffSlots.Count < 5)
					data.AutoBuffSlots.Add(false);
			if (data.UnlockedBuffSlots <= 0)
				data.UnlockedBuffSlots = 1;
			else if (data.UnlockedBuffSlots > 5)
				data.UnlockedBuffSlots = 5;
			if (data.UnlockedAutoBuffSlots < 0)
				data.UnlockedAutoBuffSlots = 0;
			else if (data.UnlockedAutoBuffSlots > 5)
				data.UnlockedAutoBuffSlots = 5;
			if (data.DisciplePercent <= 0f)
				data.DisciplePercent = 0.1f;
			data.Quests ??= new Dictionary<string, GameData.QuestRecord>();
		}

		private void CommitChanges()
		{
			if (loadedFile == null || loadedSettings == null) return;

			try
			{
                // Save GameData if present
                if (gameData != null)
                {
                    if (!string.IsNullOrEmpty(gameDataKeyLoaded))
                        loadedFile.Save(gameDataKeyLoaded, gameData);
					gameDataDirty = false;
				}

				// Save selected key object if edited
				if (selectedValue != null && selectedValueDirty && !string.IsNullOrEmpty(selectedKey))
				{
					loadedFile.Save(selectedKey, selectedValue);
					selectedValueDirty = false;
				}

                // Commit to disk
				var effective = ResolveEffectiveSettings(loadedSettings);
				loadedFile.Sync(effective);
				ShowNotification(new GUIContent("Saved"));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save ES3 file: {ex}");
                ShowNotification(new GUIContent("Save failed. See console."));
            }
        }

        private void ApplyRawJson(bool saveAfter)
        {
            if (loadedFile == null || loadedSettings == null)
                return;

            try
            {
                var effective = ResolveEffectiveSettings(loadedSettings);
                // Interpret edited text as UTF8 JSON and replace cache
                var bytes = Encoding.UTF8.GetBytes(string.IsNullOrEmpty(rawJsonEdited) ? rawJsonCache : rawJsonEdited);
                loadedFile.Clear();
                loadedFile.SaveRaw(bytes, effective);
                // Refresh derived caches
                keys = loadedFile.GetKeys() ?? Array.Empty<string>();
                rawJsonCache = loadedFile.LoadRawString();
                rawJsonEdited = string.Empty;
                if (saveAfter)
                {
                    loadedFile.Sync(effective);
                }
                ShowNotification(new GUIContent(saveAfter ? "Raw applied & saved" : "Raw applied"));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to apply raw JSON: {ex}");
                ShowNotification(new GUIContent("Raw apply failed. See console."));
        }
		}

		private void Reload()
		{
			if (loadedSettings == null)
				return;

            // Reload and auto-pick the primary key
            LoadFromSettings(loadedSettings, string.Empty, preferGameData: true);
		}

		private void OpenSaveFolder()
		{
			var dir = GetSaveFolderPath();
			if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
			{
				EditorUtility.RevealInFinder(dir);
			}
			else
			{
				ShowNotification(new GUIContent("Save folder not found"));
			}
		}

		private string GetSaveFolderPath()
		{
			try
			{
				var probe = new ES3Settings("probe.es3", ES3.Location.File);
				var full = probe.FullPath;
				return Path.GetDirectoryName(full);
			}
			catch { return Application.persistentDataPath; }
		}

		private static (string fileName, string dataKey) ComposeNames(bool beta, int betaIteration, int slot)
		{
			var prefix = beta ? $"Beta{betaIteration}" : string.Empty;
			var fileName = $"{prefix}Sd{slot}.es3";
			var dataKey = $"{prefix}Data{slot}";
			return (fileName, dataKey);
		}

        private static string FindPreferredKey(string[] fileKeys)
        {
            if (fileKeys == null || fileKeys.Length == 0) return null;
            // Prefer keys that look like Data or BetaXData, otherwise first key
            var preferred = fileKeys.FirstOrDefault(k => k.EndsWith("Data0") || k.Contains("Data"));
            return string.IsNullOrEmpty(preferred) ? fileKeys[0] : preferred;
        }

        // Helpers no longer used in simplified mode
	}
}
#endif


