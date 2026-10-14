using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine;

namespace ShroomAccess
{
    // Once per start, asks GitHub for the newest release of the mod. A newer one is said once on the
    // title screen. Ctrl+O opens its page in the browser; Ctrl+N, pressed twice, downloads and
    // installs it (Installer), for use after the next restart. A failure of the check (no connection,
    // a private or missing repository) is logged and never spoken.
    // Switched off with CheckForUpdates in UserData\MelonPreferences.cfg (ShroomAccess).
    internal static class UpdateCheck
    {
        internal static Action<string> Log = _ => { };

        // The version this build is; the packaging script reads it from here.
        internal const string Version = "0.1.1";

        private static volatile string _newer = "";
        private static volatile string _page = "";
        private static volatile string _zipUrl = "";
        private static volatile string _checksumUrl = "";
        private static bool _said, _installing, _installed;
        private static float _confirmUntil;
        private static string _gameRoot = "";
        private static readonly ConcurrentQueue<string> _toSay = new ConcurrentQueue<string>();

        internal static void Start(string repository, string gameRoot)
        {
            _gameRoot = gameRoot;
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
                    JsonElement rootEl = doc.RootElement;
                    string tag = rootEl.TryGetProperty("tag_name", out JsonElement t) ? t.GetString() ?? "" : "";
                    string page = rootEl.TryGetProperty("html_url", out JsonElement h) ? h.GetString() ?? "" : "";
                    string zip = "", sum = "";
                    if (rootEl.TryGetProperty("assets", out JsonElement assets) && assets.ValueKind == JsonValueKind.Array)
                        foreach (JsonElement asset in assets.EnumerateArray())
                        {
                            string name = asset.TryGetProperty("name", out JsonElement n) ? n.GetString() ?? "" : "";
                            string link = asset.TryGetProperty("browser_download_url", out JsonElement l) ? l.GetString() ?? "" : "";
                            if (name.StartsWith("ShroomAccess-") && name.EndsWith(".zip")) zip = link;
                            else if (name.StartsWith("ShroomAccess-") && name.EndsWith(".zip.sha256")) sum = link;
                        }
                    Log("[update] latest release " + tag + " (this is " + Version + "); zip " + (zip.Length > 0) + ", checksum " + (sum.Length > 0));
                    if (IsNewer(tag, Version)) { _page = page; _zipUrl = zip; _checksumUrl = sum; _newer = tag.TrimStart('v', 'V'); }
                }
                catch (Exception ex) { Log("[update] " + ex.GetType().Name + ": " + ex.Message); }
            });
        }

        // Called every frame on the game's thread: speech and the browser are started from here.
        internal static void Tick(bool onTitle)
        {
            while (_toSay.TryDequeue(out string line)) FocusProbe.Announce(line, interrupt: false);
            if (_newer.Length == 0) return;
            bool canInstall = _zipUrl.Length > 0 && _checksumUrl.Length > 0;
            if (onTitle && !_said)
            {
                _said = true;
                FocusProbe.Announce(Loc.T("update.available", "Shroom Access {0} is available; you have {1}.", _newer, Version)
                    + " " + (canInstall
                        ? Loc.T("update.keys_install", "Control N installs it; Control O opens its page.")
                        : Loc.T("update.keys_page", "Control O opens its download page.")), interrupt: false);
            }
            // Read every frame once announced, so the first press counts.
            bool open = _said && InputBridge.CommandKey("OpenUpdatePage");
            bool install = _said && InputBridge.CommandKey("InstallUpdate");
            if (!InputBridge.CtrlHeld) return;

            if (open && _page.StartsWith("https://github.com/"))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(_page) { UseShellExecute = true });
                    FocusProbe.Speak(Loc.T("update.opening", "Opening the download page in your browser."));
                }
                catch (Exception ex) { Log("[update] open: " + ex.Message); }
            }

            if (install && canInstall)
            {
                if (_installed) { FocusProbe.Speak(Loc.T("update.already", "Already installed. Restart the game to use it.")); return; }
                if (_installing) { FocusProbe.Speak(Loc.T("update.busy", "Still downloading.")); return; }
                // A second press within ten seconds confirms.
                if (Time.realtimeSinceStartup > _confirmUntil)
                {
                    _confirmUntil = Time.realtimeSinceStartup + 10f;
                    FocusProbe.Speak(Loc.T("update.confirm", "Press Control N again to download and install Shroom Access {0}.", _newer));
                    return;
                }
                _confirmUntil = 0f;
                _installing = true;
                FocusProbe.Speak(Loc.T("update.downloading", "Downloading Shroom Access {0}.", _newer));
                string version = _newer, zip = _zipUrl, sum = _checksumUrl, root = _gameRoot;
                Task.Run(async () =>
                {
                    string failure;
                    try { failure = await Installer.Install(zip, sum, root); }
                    catch (Exception ex) { Log("[install] " + ex); failure = Loc.T("update.fail_write", "a file could not be written"); }
                    if (failure == null)
                    {
                        _installed = true;
                        _toSay.Enqueue(Loc.T("update.done", "Shroom Access {0} is installed. Restart the game to use it.", version));
                    }
                    else _toSay.Enqueue(Loc.T("update.failed", "The update was not installed: {0}. Nothing was changed. Control O opens the page to install it by hand.", failure));
                    _installing = false;
                });
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
