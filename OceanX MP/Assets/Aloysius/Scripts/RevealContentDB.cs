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
/// Columns: <c>id, speciesName, role, blurb</c>. The parser is header-driven — reorder or add columns
/// freely; unknown columns are ignored, missing ones read as empty. Rows are indexed by BOTH their stable
/// <c>id</c> and their <c>speciesName</c>, so a lookup works whether you pass the id or the display name.
/// </summary>
public static class RevealContentDB
{
    public class Entry
    {
        public string id, speciesName, role, blurb;
    }

    const string CsvFile = "RevealContent.csv";

    static Dictionary<string, Entry> _entries;

    /// <summary>Look up a species by its stable <c>id</c> OR its display name (case-insensitive). Null if unknown.</summary>
    public static Entry Get(string idOrName)
    {
        EnsureLoaded();
        if (string.IsNullOrEmpty(idOrName)) return null;
        return _entries.TryGetValue(Norm(idOrName), out var e) ? e : null;
    }

    /// <summary>Force a re-read (called after a live download, or from a debug button).</summary>
    public static void Reload() { _entries = null; EnsureLoaded(); }

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
        if (rows.Count < 2) return;

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
                role        = Field(row, col, "role"),
                blurb       = Field(row, col, "blurb"),
            };

            // Index under both the stable id and the display name so either resolves the same entry.
            if (!string.IsNullOrWhiteSpace(id))   _entries[Norm(id)]   = entry;
            if (!string.IsNullOrWhiteSpace(name)) _entries[Norm(name)] = entry;
        }
    }

    static string Field(List<string> row, Dictionary<string, int> col, string key)
        => col.TryGetValue(key, out int i) && i < row.Count ? row[i] : "";
}
