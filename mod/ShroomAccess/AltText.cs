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
            if (_missing.Add(name)) Log("[alt-missing] " + kind + ": " + name);
            return null;
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
