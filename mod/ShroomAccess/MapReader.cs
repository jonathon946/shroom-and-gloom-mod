using System;
using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;

namespace ShroomAccess
{
    // The map is a drawing the character holds up: a chain of areas along the path, each marked with
    // what it holds, whether its fight is done, and where the player is. Nothing on it is chosen;
    // it is only read. This turns it into one line per area, nearest the start first.
    internal static class MapReader
    {
        internal static Action<string> Log = _ => { };

        internal static List<string> Lines()
        {
            var lines = new List<string>();
            try
            {
                MapUIController map = UnityEngine.Object.FindObjectOfType<MapUIController>();
                if (map == null || map.Paths == null) { lines.Add("No map to read"); return lines; }

                var segments = new List<MapSegment>();
                for (int p = 0; p < map.Paths.Count; p++)
                {
                    MapPath path = map.Paths[p];
                    if (path == null || !path.gameObject.activeInHierarchy || path.Acts == null) continue;
                    for (int a = 0; a < path.Acts.Count; a++)
                    {
                        MapAct act = path.Acts[a];
                        if (act == null || !act.gameObject.activeInHierarchy || act.Segments == null) continue;
                        for (int i = 0; i < act.Segments.Count; i++)
                        {
                            MapSegment s = act.Segments[i];
                            if (s != null && s.gameObject.activeInHierarchy) segments.Add(s);
                        }
                    }
                }
                if (segments.Count == 0) { lines.Add("The map is blank"); return lines; }

                // Tunnels at the same depth are a fork: the path splits and one of them is walked.
                var depths = new SortedDictionary<int, List<MapSegment>>();
                foreach (MapSegment s in segments)
                {
                    int depth = 0;
                    try { depth = s.TunnelDepth; } catch { }
                    if (!depths.TryGetValue(depth, out List<MapSegment> at)) depths[depth] = at = new List<MapSegment>();
                    at.Add(s);
                }

                int hereDepth = int.MinValue, known = 0;
                var raw = new System.Text.StringBuilder();
                var described = new List<KeyValuePair<int, string>>();
                foreach (var pair in depths)
                {
                    List<MapSegment> at = pair.Value;
                    at.Sort((x, y) => x.transform.position.x.CompareTo(y.transform.position.x));
                    var options = new List<string>();
                    for (int i = 0; i < at.Count; i++)
                    {
                        MapSegment s = at[i];
                        bool hidden = false, boss = false;
                        try { hidden = s._hidden; } catch { }
                        try { boss = s.IsBoss; } catch { }
                        bool isHere = s.YrHereRoot != null && s.YrHereRoot.activeInHierarchy;
                        if (isHere) hereDepth = pair.Key;
                        if (!hidden) known++;
                        raw.Append(" [" + pair.Key + " " + Code(s) + (boss ? " boss" : "") + (hidden ? " hidden" : "") + (isHere ? " here" : "") + "]");

                        var parts = new List<string>();
                        if (isHere) parts.Add("you are here");
                        if (boss) parts.Add("the boss");
                        if (hidden) parts.Add("not yet explored");
                        if (Shown(s.FinishedCombatIcon) || Shown(s.FinishedCombatIconUnrevealed)) parts.Add("fight won");
                        else if (Shown(s.CombatIcon) || Shown(s.CombatIconUnrevealed)) parts.Add("a fight");
                        if (!hidden)
                        {
                            string rewards = Rewards(s);
                            if (rewards.Length > 0) parts.Add(rewards);
                        }
                        if (parts.Count == 0) parts.Add("nothing marked");
                        string side = at.Count == 1 ? "" : at.Count == 2 ? (i == 0 ? "left: " : "right: ") : i == 0 ? "left: " : i == at.Count - 1 ? "right: " : "middle: ";
                        options.Add(side + string.Join(", ", parts));
                    }
                    described.Add(new KeyValuePair<int, string>(pair.Key,
                        "Tunnel " + pair.Key + (at.Count > 1 ? ", a fork of " + at.Count + ". " + string.Join(". ", options) : ": " + options[0])));
                }
                string rawText = raw.ToString();
                if (rawText != _lastRaw) { _lastRaw = rawText; Log("[map]" + rawText); }

                lines.Add("Map: " + depths.Count + " tunnels deep, " + segments.Count + " areas, " + known + " explored"
                    + (hereDepth != int.MinValue ? ". You are in tunnel " + hereDepth : ""));
                // From where the player is onward; what lies behind comes last and says so.
                foreach (var pair in described) if (hereDepth == int.MinValue || pair.Key >= hereDepth) lines.Add(pair.Value);
                for (int i = described.Count - 1; i >= 0; i--)
                    if (hereDepth != int.MinValue && described[i].Key < hereDepth) lines.Add("Behind you. " + described[i].Value);
            }
            catch (Exception ex)
            {
                Log("[map] " + ex.GetType().Name + ": " + ex.Message);
                lines.Add("The map could not be read");
            }
            return lines;
        }

        private static string _lastRaw = "";

        private static string Code(MapSegment s)
        {
            try { return s.TunnelCode ?? "?"; } catch { return "?"; }
        }

        private static bool Shown(SpriteRenderer r)
        {
            try { return r != null && r.enabled && r.gameObject.activeInHierarchy && r.color.a > 0.3f; } catch { return false; }
        }

        private static string Rewards(MapSegment s)
        {
            var names = new List<string>();
            try
            {
                MapSegment.MapTunnelData data = s._tunnelData;
                if (data == null) return "";
                foreach (ExploreRewardType reward in new[] { data.Reward1, data.Reward2, data.Reward3 })
                {
                    if (reward == null) continue;
                    string title = reward.UITitle;
                    if (string.IsNullOrWhiteSpace(title)) title = FocusProbe.Humanize(reward.name);
                    title = FocusProbe.Clean(title).Replace("P1 ", "").Replace("P2 ", "");
                    if (title.Length > 0 && !names.Contains(title)) names.Add(title);
                }
            }
            catch { }
            return names.Count > 0 ? "holds " + string.Join(" and ", names) : "";
        }
    }
}
