using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Loads the SHORT "new arrival" blurbs shown on the HOST / large screen from <c>RevealContent.csv</c>.
///
/// This is deliberately SEPARATE from <see cref="SpeciesContentDB"/> (which drives the TABLET info card):
/// the big screen wants a punchy one-liner ("The blacktip reef shark has returned as the top predator!"),
/// while the tablet shows the long, detailed description. Two sheets, two audiences, edited independently
/// by the fact-checkers.
///
/// Columns: <c>id, speciesName, role, sciName, FirstAddedMessage, unlockMessage, imageFile</c> — drives BOTH
/// big-screen cards: <c>FirstAddedMessage</c> for the ADDED/arrival card (SpeciesAddedReveal) and
/// <c>unlockMessage</c> for the UNLOCK card (SpeciesUnlockReveal). The parser is header-driven — reorder or add
/// columns freely; unknown columns are ignored, missing ones read as empty. Rows are indexed by BOTH their
/// stable <c>id</c> and their <c>speciesName</c>. The big-screen photos live in the HOST folder
/// (<c>StreamingAssets/Trifold</c>, holding the reveal + unlock images). The tablet loads its own images
/// from <c>StreamingAssets/Tablet</c> via <see cref="SpeciesContentDB"/>; the reveal photos are copied into
/// BOTH folders (<c>*Reveal.png</c>), so each DB reads only its own folder and they never collide.
/// </summary>
public static class RevealContentDB
{
    public class Entry
    {
        // firstAddedMessage = the ADDED/arrival card line; unlockMessage = the UNLOCK card line (may differ).
        // imageFile = the ADDED/arrival card photo; unlockImageFile = the UNLOCK card photo (both from
        // StreamingAssets/Trifold). Leave unlockImageFile blank to reuse the arrival photo.
        public string id, speciesName, role, sciName, firstAddedMessage, unlockMessage, imageFile, unlockImageFile;
    }

    const string CsvFile = "RevealContent.csv";

    /// <summary>StreamingAssets subfolder holding the big-screen images (reveal + unlock). Kept SEPARATE
    /// from the tablet's Tablet folder so the two never collide. ContentService warms it on Android.</summary>
    public const string ImageFolderName = "Trifold";

    static Dictionary<string, Entry> _entries;
    static readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

    /// <summary>Look up a species by its stable <c>id</c> OR its display name (case-insensitive). Null if unknown.</summary>
    public static Entry Get(string idOrName)
    {
        EnsureLoaded();
        if (string.IsNullOrEmpty(idOrName)) return null;
        return _entries.TryGetValue(Norm(idOrName), out var e) ? e : null;
    }

    /// <summary>Force a re-read (called after a live download, or from a debug button).</summary>
    public static void Reload() { _entries = null; _spriteCache.Clear(); EnsureLoaded(); }

    static string Norm(string s) => s == null ? "" : s.Trim().ToLowerInvariant();

    static void EnsureLoaded()
    {
        if (_entries != null) return;
        _entries = new Dictionary<string, Entry>();

        string path = ContentService.LocalPathFor(CsvFile);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[RevealContentDB] CSV not found at {path}");
            return;
        }

        string text;
        try
        {
            // FileShare.ReadWrite lets us read even while the CSV is open in Excel.
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs))
                text = sr.ReadToEnd();
        }
        catch (System.Exception ex) { Debug.LogError($"[RevealContentDB] read failed: {ex.Message}"); return; }

        var rows = CsvUtil.Parse(text);
        if (rows.Count < 2)
        {
            // Was silent. A sheet that parses to nothing means every reveal card renders blank, so say why.
            Debug.LogWarning($"[RevealContentDB] '{CsvFile}' parsed to {rows.Count} row(s) — no entries loaded, " +
                             $"the big-screen cards will fall back to their asset values. Path: {path}");
            return;
        }

        var header = rows[0];
        var col = new Dictionary<string, int>();
        for (int i = 0; i < header.Count; i++) col[header[i].Trim().ToLowerInvariant()] = i;

        for (int r = 1; r < rows.Count; r++)
        {
            var row = rows[r];
            string name = Field(row, col, "speciesname");
            string id   = Field(row, col, "id");
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(id)) continue; // blank/comment row

            var entry = new Entry
            {
                id            = id,
                speciesName   = name,
                role          = Field(row, col, "role"),
                sciName           = Field(row, col, "sciname"),
                firstAddedMessage = Field(row, col, "firstaddedmessage"),
                unlockMessage     = Field(row, col, "unlockmessage"),
                imageFile         = Field(row, col, "imagefile"),
                unlockImageFile   = Field(row, col, "unlockimagefile"),
            };

            // Index under both the stable id and the display name so either resolves the same entry.
            if (!string.IsNullOrWhiteSpace(id))   _entries[Norm(id)]   = entry;
            if (!string.IsNullOrWhiteSpace(name)) _entries[Norm(name)] = entry;
        }
    }

    static string Field(List<string> row, Dictionary<string, int> col, string key)
        => col.TryGetValue(key, out int i) && i < row.Count ? row[i] : "";

    /// <summary>Every image name this sheet references (both card columns), de-duplicated. ContentService
    /// warms these out of the APK on Android so <see cref="GetImage"/> can read them.</summary>
    public static IEnumerable<string> AllImageRefs()
    {
        EnsureLoaded();
        var seen = new HashSet<string>();
        foreach (var e in _entries.Values)   // rows are indexed twice (id + name), so de-dup
        {
            if (!string.IsNullOrWhiteSpace(e.imageFile) && seen.Add(e.imageFile)) yield return e.imageFile;
            if (!string.IsNullOrWhiteSpace(e.unlockImageFile) && seen.Add(e.unlockImageFile)) yield return e.unlockImageFile;
        }
    }

    /// <summary>Load a big-screen photo (warmed cache → baked StreamingAssets/Trifold, SEPARATE from
    /// the tablet's Tablet folder). Null if the file is missing or blank.</summary>
    public static Sprite GetImage(string imageFile)
    {
        if (string.IsNullOrWhiteSpace(imageFile)) return null;
        string key = imageFile.Trim();
        if (_spriteCache.TryGetValue(key, out var cached)) return cached;

        Sprite sprite = ContentService.LoadSprite(ImageFolderName, key);

        // Only cache a HIT — see the note in SpeciesContentDB.GetImage.
        if (sprite != null) _spriteCache[key] = sprite;
        return sprite;
    }
}
