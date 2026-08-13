using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SpeciesContentDB
{
    public class Entry
    {

        public string id, speciesName, sciName, role, iucnStatus, description, diet, habitat, funFact, imageFile, revealImageFile, iucnImage;
    }

    const string CsvFile = "SpeciesContent.csv";

    public const string ImageFolderName = "Tablet";

    static Dictionary<string, Entry> _entries;
    static readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

    public static Entry Get(string idOrName)
    {
        EnsureLoaded();
        if (string.IsNullOrEmpty(idOrName)) return null;
        return _entries.TryGetValue(Norm(idOrName), out var e) ? e : null;
    }

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

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs))
                text = sr.ReadToEnd();
        }
        catch (System.Exception ex) { Debug.LogError($"[SpeciesContentDB] read failed: {ex.Message}"); return; }

        var rows = CsvUtil.Parse(text);
        if (rows.Count < 2)
        {

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
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(id)) continue;

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

            if (!string.IsNullOrWhiteSpace(id))   _entries[Norm(id)]   = entry;
            if (!string.IsNullOrWhiteSpace(name)) _entries[Norm(name)] = entry;
        }
    }

    static string Field(List<string> row, Dictionary<string, int> col, string key)
        => col.TryGetValue(key, out int i) && i < row.Count ? row[i] : "";

    public static IEnumerable<string> AllImageRefs()
    {
        EnsureLoaded();
        var seen = new HashSet<string>();
        foreach (var e in _entries.Values)
        {
            if (!string.IsNullOrWhiteSpace(e.imageFile) && seen.Add(e.imageFile)) yield return e.imageFile;
            if (!string.IsNullOrWhiteSpace(e.revealImageFile) && seen.Add(e.revealImageFile)) yield return e.revealImageFile;
            if (!string.IsNullOrWhiteSpace(e.iucnImage) && seen.Add(e.iucnImage)) yield return e.iucnImage;
        }
    }

    public static Sprite GetImage(string imageFile)
    {
        if (string.IsNullOrWhiteSpace(imageFile)) return null;
        string key = imageFile.Trim();
        if (_spriteCache.TryGetValue(key, out var cached)) return cached;

        Sprite sprite = ContentService.LoadSprite(ImageFolderName, key);

        if (sprite != null) _spriteCache[key] = sprite;
        return sprite;
    }
}
