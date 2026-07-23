using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Loads species info cards from <c>SpeciesContent.csv</c> so the text can be fact-checked in a
/// spreadsheet (online or on disk) without touching Unity. Images load from
/// <c>StreamingAssets/Tablet/&lt;file&gt;</c> — the tablet's OWN folder, holding both the INFO photo
/// (<c>imageFile</c>, shown in the modal) and a copy of the big-screen REVEAL photo
/// (<c>revealImageFile</c>, shown on the Current-Organisms cards).
///
/// Columns: <c>id, speciesName, sciName, iucnStatus, description, diet, habitat, funFact, imageFile, revealImageFile, IUCNImage</c>.
/// The parser is header-driven — reorder or add columns freely; unknown columns are ignored, missing
/// ones read as empty. Rows are indexed by BOTH their stable <c>id</c> and their <c>speciesName</c>, so a
/// lookup works whether you pass the id (rename/translation-safe) or the display name.
/// </summary>
public static class SpeciesContentDB
{
    public class Entry
    {
        // imageFile = INFO photo (modal). revealImageFile = a copy of the big-screen REVEAL photo,
        // used on the Current-Organisms cards. Both live in StreamingAssets/Tablet.
        public string id, speciesName, sciName, role, iucnStatus, description, diet, habitat, funFact, imageFile, revealImageFile, iucnImage;
    }

    const string CsvFile = "SpeciesContent.csv";

    /// <summary>StreamingAssets subfolder holding this sheet's images (info + tablet reveal copies).
    /// ContentService warms it on Android.</summary>
    public const string ImageFolderName = "Tablet";

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
            Debug.LogWarning($"[SpeciesContentDB] CSV not found at {path}");
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
        catch (System.Exception ex) { Debug.LogError($"[SpeciesContentDB] read failed: {ex.Message}"); return; }

        var rows = CsvUtil.Parse(text);
        if (rows.Count < 2)
        {
            // Was silent. A sheet that parses to nothing means every card renders blank, so say why.
            Debug.LogWarning($"[SpeciesContentDB] '{CsvFile}' parsed to {rows.Count} row(s) — no species loaded, " +
                             $"every info card will be blank. Path: {path}");
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
                id          = id,
                speciesName = name,
                sciName     = Field(row, col, "sciname"),
                role        = Field(row, col, "role"),
                iucnStatus  = Field(row, col, "iucnstatus"),
                description = Field(row, col, "description"),
                diet        = Field(row, col, "diet"),
                habitat     = Field(row, col, "habitat"),
                funFact     = Field(row, col, "funfact"),
                imageFile   = Field(row, col, "imagefile"),
                revealImageFile = Field(row, col, "revealimagefile"),
                iucnImage   = Field(row, col, "iucnimage"),
            };

            // Index under both the stable id and the display name so either resolves the same entry.
            if (!string.IsNullOrWhiteSpace(id))   _entries[Norm(id)]   = entry;
            if (!string.IsNullOrWhiteSpace(name)) _entries[Norm(name)] = entry;
        }
    }

    static string Field(List<string> row, Dictionary<string, int> col, string key)
        => col.TryGetValue(key, out int i) && i < row.Count ? row[i] : "";

    /// <summary>Every image name this sheet references, de-duplicated. ContentService warms these out of
    /// the APK on Android so <see cref="GetImage"/> can read them.</summary>
    public static IEnumerable<string> AllImageRefs()
    {
        EnsureLoaded();
        var seen = new HashSet<string>();
        foreach (var e in _entries.Values)   // rows are indexed twice (id + name), so de-dup
        {
            if (!string.IsNullOrWhiteSpace(e.imageFile) && seen.Add(e.imageFile)) yield return e.imageFile;
            if (!string.IsNullOrWhiteSpace(e.revealImageFile) && seen.Add(e.revealImageFile)) yield return e.revealImageFile;
            if (!string.IsNullOrWhiteSpace(e.iucnImage) && seen.Add(e.iucnImage)) yield return e.iucnImage;
        }
    }

    /// <summary>Load a sprite for an entry's image (warmed cache → baked StreamingAssets). Null if missing.</summary>
    public static Sprite GetImage(string imageFile)
    {
        if (string.IsNullOrWhiteSpace(imageFile)) return null;
        string key = imageFile.Trim();
        if (_spriteCache.TryGetValue(key, out var cached)) return cached;

        Sprite sprite = ContentService.LoadSprite(ImageFolderName, key);

        // Only cache a HIT. Caching a miss would freeze a card blank for the session if it was opened
        // before ContentService finished warming the image cache on Android.
        if (sprite != null) _spriteCache[key] = sprite;
        return sprite;
    }
}
