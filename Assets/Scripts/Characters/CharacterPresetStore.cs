using System;
using System.IO;
using System.Text;
using Blindsided.SaveData;
using UnityEngine;

namespace TimelessEchoes.Characters
{
    public static class CharacterPresetStore
    {
        [Serializable] private sealed class Presets { public int version = 1; public CharacterProfile[] slots = new CharacterProfile[3]; }
        private static string FilePath => Path.Combine(SaveManager.SharedProfileDirectory, "character-presets.json");
        private static Presets Read()
        {
            if (!File.Exists(FilePath)) return new Presets();
            var data = JsonUtility.FromJson<Presets>(File.ReadAllText(FilePath));
            if (data == null || data.version != 1 || data.slots == null || data.slots.Length != 3)
                throw new IOException("The saved character presets could not be read. The existing file has been preserved.");
            return data;
        }
        public static bool TryLoad(int index, out CharacterProfile profile, out string error)
        {
            profile = null; error = null;
            try { if (index < 0 || index >= 3) throw new ArgumentOutOfRangeException(nameof(index)); profile = Read().slots[index]?.Copy(); return true; }
            catch (Exception ex) { error = ex.Message; return false; }
        }
        public static bool TrySave(int index, CharacterProfile profile, out string error)
        {
            error = null;
            try
            {
                if (index < 0 || index >= 3 || profile == null) throw new ArgumentException("Choose a valid preset slot.");
                var data = Read(); data.slots[index] = profile.Copy();
                Directory.CreateDirectory(SaveManager.SharedProfileDirectory);
                var temporary = FilePath + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(data, true), new UTF8Encoding(false));
                if (File.Exists(FilePath)) File.Replace(temporary, FilePath, FilePath + ".bak");
                else File.Move(temporary, FilePath);
                return true;
            }
            catch (Exception ex) { error = ex.Message; return false; }
        }
    }
}
