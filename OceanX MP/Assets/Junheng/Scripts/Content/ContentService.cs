using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Live-fetches the game's text content (CSV sheets) from published Google-Sheet / web URLs so
/// fact-checkers can edit the wording online and see it in the game on the next launch — with NO
/// rebuild, on the host AND the tablet.
///
/// Fallback chain (never fails):
///   1. Remote URL (this component)  ->  cached to <see cref="CacheDir"/>
///   2. Last cached copy (persistentDataPath — readable on Windows AND Android)
///   3. Baked file in StreamingAssets (editable on the Windows host; unreadable on Android)
///   4. The hardcoded strings in the loaders themselves
///
/// IMAGES work the same way, but they are never downloaded — they ship baked in the build. The catch
/// is that on Android "baked in the build" means "inside the APK", reachable only as a jar: URI that
/// File.IO cannot touch. So at startup we copy every image the sheets reference OUT of the APK and
/// into <see cref="CacheDir"/> once (see WarmImages); after that the loaders read them with plain
/// File.ReadAllBytes, identically on Windows and Android. On desktop StreamingAssets is a real folder,
/// so the copy is skipped entirely and the loaders read it directly.
///
/// SETUP:
///   • Put ONE of these on an always-active object in every scene that shows content
///     (host scene: for Alucia + species cards; tablet scene: for the tablet-text sheet later).
///   • For each CSV, add a Source with the file name and the sheet's published-CSV URL:
///       Google Sheets → File → Share → Publish to web → (pick the tab) → Comma-separated values (.csv).
///   • Leave a URL blank (or turn off <see cref="enableRemoteFetch"/>) to use only the baked/cached file.
///     Images are still warmed in that case — they do not depend on the network at all.
/// </summary>
public class ContentService : MonoBehaviour
{
    [System.Serializable]
    public class Source
    {
        [Tooltip("Exact file name the loader reads, e.g. \"alucia_lines.csv\" or \"SpeciesContent.csv\".")]
        public string fileName;

        [Tooltip("Published-to-web CSV URL for this sheet/tab. Leave blank to skip (uses baked/cached copy).")]
        [TextArea] public string publishedCsvUrl;
    }

    [Header("Master switch")]
    [Tooltip("Off = never touch the network; the game uses only the cached/baked files.")]
    public bool enableRemoteFetch = true;

    [Header("Fetch behaviour")]
    [Tooltip("Seconds to wait for each download before giving up and keeping the cached/baked copy.")]
    public float timeoutSeconds = 6f;

    [Tooltip("Re-download every N seconds while the app runs (0 = only once at startup).")]
    public float refreshIntervalSeconds = 0f;

    [Header("Content sheets")]
    public List<Source> sources = new List<Source>();

    /// <summary>Writable folder where downloaded copies are cached (works on Windows and Android).</summary>
    public static string CacheDir => Path.Combine(Application.persistentDataPath, "ContentCache");

    /// <summary>
    /// Best available LOCAL path for a content file: the fresh cached download if present,
    /// otherwise the baked StreamingAssets copy. Loaders should read from this.
    /// (On Android the StreamingAssets copy is inside the APK and not directly readable, so the
    /// cache — populated by a successful fetch — is what makes tablet content live-editable.)
    /// </summary>
    public static string LocalPathFor(string fileName)
    {
        string cached = Path.Combine(CacheDir, fileName);
        if (File.Exists(cached)) return cached;
        return Path.Combine(Application.streamingAssetsPath, fileName);
    }

    /// <summary>
    /// True when <see cref="Application.streamingAssetsPath"/> is a real folder that File.IO can read
    /// (Windows/editor/Mac). False on Android, where it is a "jar:file://…!/assets" URI pointing inside
    /// the compressed APK — File.Exists there is ALWAYS false, which is why images need warming.
    /// </summary>
    public static bool StreamingAssetsIsDirectlyReadable => !Application.streamingAssetsPath.Contains("://");

    /// <summary>Where a warmed copy of <paramref name="folder"/>/<paramref name="fileName"/> lives.</summary>
    static string CachedImagePath(string folder, string fileName) => Path.Combine(CacheDir, folder, fileName);

    /// <summary>
    /// Best available LOCAL path for an image in a StreamingAssets subfolder: the warmed cache copy if
    /// present, otherwise the baked StreamingAssets path (which only resolves on desktop). Null when the
    /// name is blank. Mirrors <see cref="LocalPathFor"/> for the CSVs.
    /// </summary>
    public static string LocalImagePathFor(string folder, string fileName)
    {
        if (string.IsNullOrWhiteSpace(folder) || string.IsNullOrWhiteSpace(fileName)) return null;
        string file = fileName.Trim();

        string cached = CachedImagePath(folder, file);
        if (File.Exists(cached)) return cached;

        return Path.Combine(Application.streamingAssetsPath, folder, file);
    }

    /// <summary>
    /// Loads a sprite for an image in a StreamingAssets subfolder, via <see cref="LocalImagePathFor"/>.
    /// Returns null (quietly) when the image isn't available yet — callers must NOT cache that null, or a
    /// pre-warm miss would stick for the whole session. Shared by SpeciesContentDB and RevealContentDB so
    /// there is exactly one place that knows how to turn a content image into a Sprite.
    /// </summary>
    public static Sprite LoadSprite(string folder, string fileName)
    {
        string path = LocalImagePathFor(folder, fileName);
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes)) return null;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[ContentService] image load failed '{folder}/{fileName}': {ex.Message}");
            return null;
        }
    }

    void Start()
    {
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        do
        {
            if (enableRemoteFetch)
            {
                for (int i = 0; i < sources.Count; i++)
                {
                    Source s = sources[i];
                    if (s == null || string.IsNullOrWhiteSpace(s.fileName) || string.IsNullOrWhiteSpace(s.publishedCsvUrl))
                        continue;
                    yield return Fetch(s);
                }
            }

            // Images are warmed even when remote fetch is off or every download failed: they are baked
            // into the build, not downloaded, and on Android they are unreadable until copied out of the
            // APK. Runs after the fetch so it warms whatever the FRESH sheet references.
            for (int i = 0; i < sources.Count; i++)
            {
                Source s = sources[i];
                if (s == null || string.IsNullOrWhiteSpace(s.fileName)) continue;
                yield return WarmImagesFor(s.fileName);
            }

            if (refreshIntervalSeconds > 0f) yield return new WaitForSeconds(refreshIntervalSeconds);
        }
        while (refreshIntervalSeconds > 0f);
    }

    IEnumerator Fetch(Source s)
    {
        using (UnityWebRequest req = UnityWebRequest.Get(s.publishedCsvUrl.Trim()))
        {
            req.timeout = Mathf.Max(1, Mathf.CeilToInt(timeoutSeconds));
            req.redirectLimit = 32; // published-sheet CSV 307-redirects to googleusercontent — follow it
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[ContentService] Fetch failed for '{s.fileName}': {req.error} (HTTP {req.responseCode}). Keeping cached/baked copy.");
                yield break;
            }

            string body = req.downloadHandler.text;
            if (!LooksLikeCsv(body))
            {
                // Show what actually came back so a truncated / wrong link is obvious. A Google published-CSV
                // URL MUST end in '&output=csv'; without it Google returns an HTML page (starts with '<').
                string peek = string.IsNullOrEmpty(body)
                    ? "(empty body)"
                    : body.Substring(0, Mathf.Min(120, body.Length)).Replace("\r", " ").Replace("\n", " ");
                Debug.LogWarning($"[ContentService] '{s.fileName}' download wasn't CSV — ignored (kept cached/baked). " +
                                 $"Make sure the URL ends in '&output=csv'. HTTP {req.responseCode} · url={req.url} · body starts: {peek}");
                yield break;
            }

            try
            {
                Directory.CreateDirectory(CacheDir);
                File.WriteAllText(Path.Combine(CacheDir, s.fileName), body);
                Debug.Log($"[ContentService] Updated '{s.fileName}' from remote ({body.Length} chars).");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ContentService] Could not cache '{s.fileName}': {e.Message}");
                yield break;
            }

            NotifyReload(s.fileName);
        }
    }

    // A wrong/unpublished Google Sheet link returns an HTML page, not CSV. Don't cache that over good data.
    static bool LooksLikeCsv(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return false;
        string head = body.TrimStart();
        return !head.StartsWith("<"); // HTML error page starts with '<'
    }

    // Same idea as LooksLikeCsv, for bytes: never cache something that isn't actually an image.
    static bool LooksLikeImage(byte[] d)
    {
        if (d == null || d.Length < 4) return false;
        if (d[0] == 0x89 && d[1] == 0x50 && d[2] == 0x4E && d[3] == 0x47) return true; // PNG
        if (d[0] == 0xFF && d[1] == 0xD8 && d[2] == 0xFF) return true;                 // JPEG
        return false;
    }

    // Identifies the build a cached image folder was filled from. The warmed copies live in
    // persistentDataPath, which SURVIVES app updates — so without this a new APK carrying a replaced
    // photo would keep serving the previous install's stale copy forever (the warm loop skips files that
    // already exist). buildGUID changes every build; it is empty in the editor, which never warms anyway.
    static string BuildStamp => string.IsNullOrEmpty(Application.buildGUID) ? "editor" : Application.buildGUID;

    // Maps a content sheet to the image folder + file names it references. Same hardcoded shape as
    // NotifyReload — add a case here when a new sheet brings its own images.
    IEnumerator WarmImagesFor(string csvFileName)
    {
        switch (csvFileName)
        {
            case "SpeciesContent.csv":
                yield return WarmImages(SpeciesContentDB.ImageFolderName, SpeciesContentDB.AllImageRefs());
                break;
            case "RevealContent.csv":
                yield return WarmImages(RevealContentDB.ImageFolderName, RevealContentDB.AllImageRefs());
                break;
        }

        // Drop the loaders' sprite caches so anything opened before the warm finished re-reads from disk.
        NotifyReload(csvFileName);
    }

    /// <summary>
    /// Copies every image in <paramref name="fileNames"/> out of the baked StreamingAssets folder and into
    /// <see cref="CacheDir"/>, so File.IO can read them on Android. No-op on desktop (StreamingAssets is a
    /// real folder there). Each file is copied ONCE and skipped on later launches; the whole folder is
    /// dropped when the build changes. A failure is logged and skipped — the card falls back to blank
    /// rather than taking the app down.
    /// </summary>
    IEnumerator WarmImages(string folder, IEnumerable<string> fileNames)
    {
        if (StreamingAssetsIsDirectlyReadable) yield break;   // desktop: loaders read StreamingAssets direct
        if (string.IsNullOrWhiteSpace(folder) || fileNames == null) yield break;

        string dir = Path.Combine(CacheDir, folder);
        string stampPath = Path.Combine(dir, ".build");
        string stamp = BuildStamp;

        // New build => throw away the previous install's copies before re-warming.
        // (No yield inside this try — 'yield' is illegal in a try that has a catch.)
        bool prepared = false;
        try
        {
            bool stale = !File.Exists(stampPath) || File.ReadAllText(stampPath) != stamp;
            if (stale && Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            prepared = true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ContentService] Could not prepare image cache '{folder}': {e.Message}");
        }
        if (!prepared) yield break;

        int copied = 0, failed = 0;
        var seen = new HashSet<string>();

        foreach (string raw in fileNames)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            string file = raw.Trim();
            if (!seen.Add(file)) continue;                    // sheet indexes rows twice (id + name)

            string dest = CachedImagePath(folder, file);
            if (File.Exists(dest)) continue;                  // already warmed by an earlier launch

            // Build the jar: URI by hand — Path.Combine would be fine on Android but mangles a URI on
            // any platform whose separator isn't '/'. UnityWebRequest is the only reader for jar:.
            string src = $"{Application.streamingAssetsPath}/{folder}/{file}";

            using (UnityWebRequest req = UnityWebRequest.Get(src))
            {
                req.timeout = Mathf.Max(1, Mathf.CeilToInt(timeoutSeconds));
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[ContentService] Image '{folder}/{file}' not readable from the build: {req.error}. " +
                                     $"Is it actually in Assets/StreamingAssets/{folder}/ ? (referenced by the sheet)");
                    failed++;
                    continue;
                }

                byte[] data = req.downloadHandler.data;
                if (!LooksLikeImage(data))
                {
                    Debug.LogWarning($"[ContentService] '{folder}/{file}' isn't a PNG/JPEG ({(data == null ? 0 : data.Length)} bytes) — skipped.");
                    failed++;
                    continue;
                }

                try { File.WriteAllBytes(dest, data); copied++; }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[ContentService] Could not cache image '{folder}/{file}': {e.Message}");
                    failed++;
                }
            }
        }

        try { File.WriteAllText(stampPath, stamp); } catch { /* stamp is an optimisation, not correctness */ }

        if (copied > 0 || failed > 0)
            Debug.Log($"[ContentService] Image cache '{folder}': {copied} copied, {failed} failed, {seen.Count} referenced.");
    }

    // Tell the matching loader to drop its cache so the next read picks up the fresh file.
    static void NotifyReload(string fileName)
    {
        switch (fileName)
        {
            case "alucia_lines.csv":   AluciaLines.Reload();     break;
            case "SpeciesContent.csv": SpeciesContentDB.Reload(); break; // TABLET info card (long)
            case "RevealContent.csv":  RevealContentDB.Reload();  break; // HOST/big-screen arrival blurb (short)
            // Future tablet-text sheet: add its loader's Reload() here.
        }
    }
}
