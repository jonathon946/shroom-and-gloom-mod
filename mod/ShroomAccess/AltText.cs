using System;
using System.Collections.Generic;
using System.IO;

namespace ShroomAccess
{
    // Written descriptions of what things look like, for the buffer. The game's art carries much of
    // its character and has no text of its own, so these are authored by us, one line per thing, in
    // alt-text.txt beside the mod:   Name = what it looks like
    // Anything looked up and not found is noted once in the log, which is the list of what to write next.
    internal static class AltText
    {
        internal static Action<string> Log = _ => { };

        private static Dictionary<string, string> _table;
        private static readonly HashSet<string> _missing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static string _path;

        internal static void Init(string modsDir)
        {
            _path = Path.Combine(modsDir, "ShroomAccess", "alt-text.txt");
        }

        internal static string Lookup(string kind, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            name = name.Trim();
            if (_table == null) Load();
            if (_table.TryGetValue(name, out string text)) return text;
            if (_missing.Add(name))
            {
                Log("[alt-missing] " + kind + ": " + name);
                QueueShot(kind, name);
            }
            return null;
        }

        // On the developer's machine, a thing with no description yet gets its picture taken the
        // first time it is focused, so the description can be written from it later.
        private static string _shotDir;
        private static string _shotPath;
        private static float _shotAt;

        internal static void EnableShots(string userDataDir)
        {
            _shotDir = Path.Combine(userDataDir, "ShroomAccess", "alt-shots");
        }

        private static void QueueShot(string kind, string name)
        {
            if (_shotDir == null) return;
            var safe = new System.Text.StringBuilder();
            foreach (char c in name) safe.Append(char.IsLetterOrDigit(c) ? c : '-');
            string path = Path.Combine(_shotDir, kind + "-" + safe + ".png");
            if (File.Exists(path)) return;
            _shotPath = path;
            // Let the card or creature finish moving into view first.
            _shotAt = UnityEngine.Time.realtimeSinceStartup + 0.9f;
        }

        internal static void Tick()
        {
            if (_shotPath == null || UnityEngine.Time.realtimeSinceStartup < _shotAt) return;
            try
            {
                Directory.CreateDirectory(_shotDir);
                UnityEngine.ScreenCapture.CaptureScreenshot(_shotPath);
                Log("[alt-shot] " + _shotPath);
            }
            catch (Exception ex) { Log("[alt-shot] failed: " + ex.Message); }
            _shotPath = null;
        }

        private static void Load()
        {
            _table = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (_path == null || !File.Exists(_path)) { Log("Alt text file not found: " + _path); return; }
                foreach (string raw in File.ReadAllLines(_path))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    _table[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
                }
                Log("Alt text: " + _table.Count + " descriptions.");
            }
            catch (Exception ex) { Log("Alt text failed to load: " + ex.Message); }
        }
    }
}
