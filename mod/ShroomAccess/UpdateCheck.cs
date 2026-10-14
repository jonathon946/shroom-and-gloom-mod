using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ShroomAccess
{
    // Once per start, asks GitHub for the newest release of the mod. A newer one is said once on the
    // title screen; Ctrl+O then opens its page in the browser. Nothing is downloaded or installed.
    // A failure (no connection, a private or missing repository) is logged and never spoken.
    // It can be switched off in UserData\MelonPreferences.cfg (ShroomAccess, CheckForUpdates).
    internal static class UpdateCheck
    {
        internal static Action<string> Log = _ => { };

        // The version this build is; the packaging script reads it from here.
        internal const string Version = "0.1.0";

        private static volatile string _newer = "";
        private static volatile string _page = "";
        private static bool _said;

        internal static void Start(string repository)
        {
            if (string.IsNullOrWhiteSpace(repository)) return;
            Task.Run(async () =>
            {
                try
                {
                    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                    // GitHub refuses requests without a user agent.
                    http.DefaultRequestHeaders.UserAgent.ParseAdd("ShroomAccess/" + Version);
                    http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
                    string url = "https://api.github.com/repos/" + repository.Trim() + "/releases/latest";
                    using HttpResponseMessage response = await http.GetAsync(url);
                    if (!response.IsSuccessStatusCode) { Log("[update] " + url + ": " + (int)response.StatusCode); return; }
                    using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    string tag = doc.RootElement.TryGetProperty("tag_name", out JsonElement t) ? t.GetString() ?? "" : "";
                    string page = doc.RootElement.TryGetProperty("html_url", out JsonElement h) ? h.GetString() ?? "" : "";
                    Log("[update] latest release " + tag + " (this is " + Version + ")");
                    if (IsNewer(tag, Version)) { _page = page; _newer = tag.TrimStart('v', 'V'); }
                }
                catch (Exception ex) { Log("[update] " + ex.GetType().Name + ": " + ex.Message); }
            });
        }

        // Called every frame on the game's thread: speech and the browser are started from here.
        internal static void Tick(bool onTitle)
        {
            if (_newer.Length == 0) return;
            if (onTitle && !_said)
            {
                _said = true;
                FocusProbe.Announce("Shroom Access " + _newer + " is available; you have " + Version + ". Control O opens its download page.", interrupt: false);
            }
            // Read every frame once announced, so the first press counts.
            bool pressed = _said && InputBridge.NamedKey("o");
            if (pressed && InputBridge.CtrlHeld && _page.StartsWith("https://github.com/"))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(_page) { UseShellExecute = true });
                    FocusProbe.Speak("Opening the download page in your browser.");
                }
                catch (Exception ex) { Log("[update] open: " + ex.Message); }
            }
        }

        private static bool IsNewer(string tag, string current)
        {
            return System.Version.TryParse(tag.TrimStart('v', 'V'), out System.Version latest)
                && System.Version.TryParse(current, out System.Version mine)
                && latest > mine;
        }
    }
}
