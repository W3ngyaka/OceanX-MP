using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Loads species content (text + image filename) from a CSV in StreamingAssets so the
// content can be edited in a spreadsheet without touching Unity. Images are loaded from
// StreamingAssets/SpeciesImages/<file> at runtime. Keyed by species name (case-insensitive).
public static class SpeciesContentDB
{
    public class Entry
    {
        public string speciesName, sciName, role, iucnStatus, description, diet, habitat, funFact, imageFile, iucnImage;
    }

    const string CsvFile = "SpeciesContent.csv";
    const string ImageFolder = "SpeciesImages";

    static Dictionary<string, Entry> _entries;
    static readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

    public static Entry Get(string speciesName)
    {
        EnsureLoaded();
        if (string.IsNullOrEmpty(speciesName)) return null;
        return _entries.TryGetValue(Norm(speciesName), out var e) ? e : null;
    }

    // Force a re-read (handy after editing the CSV; call from a debug button if wanted).
    public static void Reload() { _entries = null; _spriteCache.Clear(); EnsureLoaded(); }

    static string Norm(string s) => s == null ? "" : s.Trim().ToLowerInvariant();

    static void EnsureLoaded()
    {
        if (_entries != null) return;
        _entries = new Dictionary<string, Entry>();

        string path = Path.Combine(Application.streamingAssetsPath, CsvFile);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[SpeciesContentDB] CSV not found at {path}");
            return;
        }

        string text;
        try
        {
            // FileShare.ReadWrite lets us read even while the CSV is open in Excel (which
            // otherwise locks it and throws a sharing violation).
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs))
                text = sr.ReadToEnd();
        }
        catch (System.Exception ex) { Debug.LogError($"[SpeciesContentDB] read failed: {ex.Message}"); return; }

        var rows = ParseCsv(text);
        if (rows.Count < 2) return;

        var header = rows[0];
        var col = new Dictionary<string, int>();
        for (int i = 0; i < header.Count; i++) col[header[i].Trim().ToLowerInvariant()] = i;

        for (int r = 1; r < rows.Count; r++)
        {
            var row = rows[r];
            string name = Field(row, col, "speciesname");
            if (string.IsNullOrWhiteSpace(name)) continue;
            _entries[Norm(name)] = new Entry
            {
                speciesName = name,
                sciName     = Field(row, col, "sciname"),
                role        = Field(row, col, "role"),
                iucnStatus  = Field(row, col, "iucnstatus"),
                description = Field(row, col, "description"),
                diet        = Field(row, col, "diet"),
                habitat     = Field(row, col, "habitat"),
                funFact     = Field(row, col, "funfact"),
                imageFile   = Field(row, col, "imagefile"),
                iucnImage   = Field(row, col, "iucnimage"),
            };
        }
    }

    static string Field(List<string> row, Dictionary<string, int> col, string key)
        => col.TryGetValue(key, out int i) && i < row.Count ? row[i] : "";

    // Load a sprite for an entry's image from StreamingAssets/SpeciesImages. Cached; null if missing.
    public static Sprite GetImage(string imageFile)
    {
        if (string.IsNullOrWhiteSpace(imageFile)) return null;
        if (_spriteCache.TryGetValue(imageFile, out var cached)) return cached;

        string path = Path.Combine(Application.streamingAssetsPath, ImageFolder, imageFile);
        Sprite sprite = null;
        if (File.Exists(path))
        {
            try
            {
                var bytes = File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                    sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
            catch (System.Exception ex) { Debug.LogWarning($"[SpeciesContentDB] image load failed '{imageFile}': {ex.Message}"); }
        }
        _spriteCache[imageFile] = sprite; // cache even null so we don't re-hit disk each open
        return sprite;
    }

    // --- Minimal RFC-4180-ish CSV parser: handles quotes, embedded commas/newlines, "" escapes ---
    static List<List<string>> ParseCsv(string text)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var sb = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"') { sb.Append('"'); i++; }
                    else inQuotes = false;
                }
                else sb.Append(c);
            }
            else
            {
                if (c == '"') inQuotes = true;
                else if (c == ',') { row.Add(sb.ToString()); sb.Clear(); }
                else if (c == '\r') { }
                else if (c == '\n') { row.Add(sb.ToString()); sb.Clear(); rows.Add(row); row = new List<string>(); }
                else sb.Append(c);
            }
        }
        if (sb.Length > 0 || row.Count > 0) { row.Add(sb.ToString()); rows.Add(row); }
        rows.RemoveAll(r => r.Count == 1 && string.IsNullOrEmpty(r[0]));
        return rows;
    }
}
