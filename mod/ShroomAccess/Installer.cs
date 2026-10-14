using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ShroomAccess
{
    // Downloads a release zip of the mod from GitHub and puts its files into the game folder.
    // The zip must match the SHA256 published beside it in the same release, or nothing is written.
    // Only the mod's own files are taken (Mods\ShroomAccess.dll, Mods\ShroomAccess\..., UserLibs\prism...);
    // anything else in the zip, the README among it, is ignored. Files the running game has open cannot be
    // overwritten but can be renamed, so each existing file is moved aside to "<name>.old" first and
    // the new one written in its place; the old ones are deleted on the next start. If any write
    // fails, the files moved aside are put back. The new version runs after a restart.
    internal static class Installer
    {
        internal static Action<string> Log = _ => { };

        private const long MaxZipBytes = 50L * 1024 * 1024;

        // Called on start: the files an earlier install moved aside. Only the mod's own: other
        // mods may keep files of the same ending.
        internal static void CleanUp(string gameRoot)
        {
            var candidates = new List<string>
            {
                Path.Combine(gameRoot, "Mods", "ShroomAccess.dll.old"),
                Path.Combine(gameRoot, "UserLibs", "prism.dll.old"),
            };
            foreach (string dir in new[] { Path.Combine(gameRoot, "Mods", "ShroomAccess"), Path.Combine(gameRoot, "UserLibs", "prism-licenses") })
            {
                try { if (Directory.Exists(dir)) candidates.AddRange(Directory.GetFiles(dir, "*.old", SearchOption.AllDirectories)); }
                catch { }
            }
            foreach (string old in candidates)
            {
                try { if (File.Exists(old)) { File.Delete(old); Log("[install] removed " + old); } } catch { }
            }
        }

        // Returns null on success, or the reason in plain words (for speech) on failure.
        internal static async Task<string> Install(string zipUrl, string checksumUrl, string gameRoot)
        {
            if (!zipUrl.StartsWith("https://github.com/") && !zipUrl.StartsWith("https://objects.githubusercontent.com/"))
                return Loc.T("update.fail_source", "the download is not on GitHub");
            if (string.IsNullOrEmpty(checksumUrl))
                return Loc.T("update.fail_no_checksum", "the release has no checksum file to check the download against");

            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("ShroomAccess/" + UpdateCheck.Version);

            string expected;
            try
            {
                string text = await http.GetStringAsync(checksumUrl);
                expected = text.Trim().Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
                expected = expected.ToLowerInvariant();
            }
            catch (Exception ex) { Log("[install] checksum: " + ex.Message); return Loc.T("update.fail_download", "the download did not complete"); }
            if (expected.Length != 64) return Loc.T("update.fail_no_checksum", "the release has no checksum file to check the download against");

            byte[] zip;
            try
            {
                using HttpResponseMessage response = await http.GetAsync(zipUrl);
                if (!response.IsSuccessStatusCode) { Log("[install] zip: " + (int)response.StatusCode); return Loc.T("update.fail_download", "the download did not complete"); }
                if (response.Content.Headers.ContentLength > MaxZipBytes) return Loc.T("update.fail_download", "the download did not complete");
                zip = await response.Content.ReadAsByteArrayAsync();
            }
            catch (Exception ex) { Log("[install] zip: " + ex.Message); return Loc.T("update.fail_download", "the download did not complete"); }
            if (zip.Length > MaxZipBytes) return Loc.T("update.fail_download", "the download did not complete");

            string actual;
            using (SHA256 sha = SHA256.Create()) actual = string.Concat(sha.ComputeHash(zip).Select(b => b.ToString("x2")));
            if (actual != expected)
            {
                Log("[install] checksum mismatch: expected " + expected + ", got " + actual);
                return Loc.T("update.fail_checksum", "the download does not match its checksum");
            }

            return InstallZip(zip, gameRoot);
        }

        // The checked zip into the game folder. Also used by the dev bridge to test with a local zip.
        internal static string InstallZip(byte[] zip, string gameRoot)
        {
            // Read everything first; write only if the whole zip is sound.
            var files = new List<KeyValuePair<string, byte[]>>();
            try
            {
                using var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read);
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string name = entry.FullName.Replace('\\', '/');
                    if (name.EndsWith("/")) continue;
                    if (!Allowed(name)) { Log("[install] skipped " + name); continue; }
                    using var s = entry.Open();
                    using var m = new MemoryStream();
                    s.CopyTo(m);
                    files.Add(new KeyValuePair<string, byte[]>(name, m.ToArray()));
                }
            }
            catch (Exception ex) { Log("[install] unzip: " + ex.Message); return Loc.T("update.fail_zip", "the download is not a readable zip"); }
            if (!files.Any(f => f.Key == "Mods/ShroomAccess.dll")) return Loc.T("update.fail_zip", "the download is not a readable zip");

            string root = Path.GetFullPath(gameRoot);
            var movedAside = new List<string>();
            var written = new List<string>();
            try
            {
                foreach (var file in files)
                {
                    string target = Path.GetFullPath(Path.Combine(root, file.Key.Replace('/', Path.DirectorySeparatorChar)));
                    if (!target.StartsWith(root + Path.DirectorySeparatorChar)) continue;
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    if (File.Exists(target))
                    {
                        string aside = target + ".old";
                        if (File.Exists(aside)) File.Delete(aside);
                        File.Move(target, aside);
                        movedAside.Add(target);
                    }
                    File.WriteAllBytes(target, file.Value);
                    written.Add(target);
                }
                Log("[install] wrote " + written.Count + " files");
                return null;
            }
            catch (Exception ex)
            {
                Log("[install] write failed, putting back: " + ex.GetType().Name + ": " + ex.Message);
                foreach (string target in written.Where(t => !movedAside.Contains(t))) { try { File.Delete(target); } catch { } }
                foreach (string target in movedAside)
                {
                    try
                    {
                        if (File.Exists(target)) File.Delete(target);
                        File.Move(target + ".old", target);
                    }
                    catch (Exception back) { Log("[install] could not put back " + target + ": " + back.Message); }
                }
                return ex is UnauthorizedAccessException
                    ? Loc.T("update.fail_access", "the game folder cannot be written to")
                    : Loc.T("update.fail_write", "a file could not be written");
            }
        }

        // The mod's own files only.
        private static bool Allowed(string name)
        {
            if (name.Contains("..") || name.StartsWith("/") || name.Contains(":")) return false;
            return name == "Mods/ShroomAccess.dll"
                || name.StartsWith("Mods/ShroomAccess/")
                || name == "UserLibs/prism.dll"
                || name.StartsWith("UserLibs/prism-licenses/");
        }
    }
}
