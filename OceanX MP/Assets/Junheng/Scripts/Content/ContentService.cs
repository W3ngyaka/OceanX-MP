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
/// SETUP:
///   • Put ONE of these on an always-active object in every scene that shows content
///     (host scene: for Alucia + species cards; tablet scene: for the tablet-text sheet later).
///   • For each CSV, add a Source with the file name and the sheet's published-CSV URL:
///       Google Sheets → File → Share → Publish to web → (pick the tab) → Comma-separated values (.csv).
///   • Leave a URL blank (or turn off <see cref="enableRemoteFetch"/>) to use only the baked/cached file.
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

    void Start()
    {
        if (enableRemoteFetch) StartCoroutine(FetchLoop());
    }

    IEnumerator FetchLoop()
    {
        do
        {
            for (int i = 0; i < sources.Count; i++)
            {
                Source s = sources[i];
                if (s == null || string.IsNullOrWhiteSpace(s.fileName) || string.IsNullOrWhiteSpace(s.publishedCsvUrl))
                    continue;
                yield return Fetch(s);
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
