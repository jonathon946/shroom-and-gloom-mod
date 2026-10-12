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

                int here = -1, known = 0;
                var described = new List<string>();
                for (int i = 0; i < segments.Count; i++)
                {
                    MapSegment s = segments[i];
                    bool hidden = false;
                    try { hidden = s._hidden; } catch { }
                    bool isHere = s.YrHereRoot != null && s.YrHereRoot.activeInHierarchy;
                    if (isHere) here = i;
                    if (!hidden) known++;

                    var parts = new List<string>();
                    if (isHere) parts.Add("you are here");
                    if (hidden) parts.Add("not yet explored");
                    if (Shown(s.FinishedCombatIcon) || Shown(s.FinishedCombatIconUnrevealed)) parts.Add("fight won");
                    else if (Shown(s.CombatIcon) || Shown(s.CombatIconUnrevealed)) parts.Add("a fight");
                    if (!hidden)
                    {
                        string rewards = Rewards(s);
                        if (rewards.Length > 0) parts.Add(rewards);
                    }
                    described.Add("Area " + (i + 1) + " of " + segments.Count + (parts.Count > 0 ? ": " + string.Join(", ", parts) : ""));
                }

                lines.Add("Map: " + segments.Count + " areas on this path, " + known + " explored"
                    + (here >= 0 ? ". You are in area " + (here + 1) : ""));
                // Start reading from where the player is, then onward, then back.
                if (here >= 0)
                {
                    for (int i = here; i < described.Count; i++) lines.Add(described[i]);
                    for (int i = here - 1; i >= 0; i--) lines.Add(described[i]);
                }
                else lines.AddRange(described);
            }
            catch (Exception ex)
            {
                Log("[map] " + ex.GetType().Name + ": " + ex.Message);
                lines.Add("The map could not be read");
            }
            return lines;
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
