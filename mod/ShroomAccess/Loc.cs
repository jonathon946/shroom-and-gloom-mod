using System;
using System.Collections.Generic;
using System.IO;

namespace ShroomAccess
{
    // The one table of everything the mod says in its own words. Each call names a key and gives
    // the English; a strings.txt beside the mod (Mods\ShroomAccess\strings.txt, lines of
    // "key = text", {0} {1} for the values) replaces the English for those keys. Without the file,
    // or for a key it lacks, the English is said. tools\Export-Strings.ps1 writes every key and its
    // English to a template for translators. Text read from the game itself is already in the
    // game's language and does not go through here.
    internal static class Loc
    {
        internal static Action<string> Log = _ => { };

        private static readonly Dictionary<string, string> _table = new Dictionary<string, string>();

        internal static void Load(string path)
        {
            _table.Clear();
            try
            {
                if (!File.Exists(path)) return;
                foreach (string raw in File.ReadAllLines(path))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    int eq = line.IndexOf(" = ", StringComparison.Ordinal);
                    if (eq <= 0) continue;
                    _table[line.Substring(0, eq).Trim()] = line.Substring(eq + 3).Replace("\\n", "\n");
                }
                Log("Strings: " + _table.Count + " from " + path);
            }
            catch (Exception ex) { Log("Strings not loaded: " + ex.Message); }
        }

        internal static string T(string key, string english, params object[] args)
        {
            string text = _table.TryGetValue(key, out string translated) ? translated : english;
            if (args == null || args.Length == 0) return text;
            try { return string.Format(text, args); }
            catch (FormatException)
            {
                // A translation with broken placeholders falls back to the English.
                Log("[strings] bad placeholders in " + key);
                return string.Format(english, args);
            }
        }
    }
}
