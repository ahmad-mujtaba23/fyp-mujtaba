using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Osmosis
{
    [Serializable]
    public struct Reading
    {
        public float concentration;   // % sugar
        public float initial, final;  // mm
        public float Change => final - initial;
    }

    [Serializable]
    public class SaveData
    {
        public List<Reading> readings = new List<Reading>();
        public bool hasBestIsotonic;
        public float bestIsotonic;
    }

    /// JSON save in persistentDataPath via JsonUtility (no packages). Writes to a temp file first so a crash
    /// mid-save never leaves a half-written file; a missing or corrupt file loads as empty instead of throwing.
    public static class SaveService
    {
        public static string DefaultPath => Path.Combine(Application.persistentDataPath, "osmosis_save.json");

        public static void Save(SaveData data, string path = null)
        {
            path ??= DefaultPath;
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(data));
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }

        public static SaveData Load(string path = null)
        {
            path ??= DefaultPath;
            try
            {
                if (File.Exists(path)) return JsonUtility.FromJson<SaveData>(File.ReadAllText(path)) ?? new SaveData();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Osmosis] save file unreadable, starting fresh: " + e.Message);
            }
            return new SaveData();
        }
    }
}
