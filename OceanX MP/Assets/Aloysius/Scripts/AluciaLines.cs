using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Loads Alucia's dialogue from <c>alucia_lines.csv</c> so non-developers (fact-checkers) can edit
/// what she says in a spreadsheet — online or on disk — without opening Unity.
///
/// The CSV is EVENT + VARIANT based: columns are <c>Event, Species, Mood, Weight, Text, Notes</c>.
///  • <b>Event</b> is the stable ID the game matches on (e.g. <c>health.critical</c>, <c>species.overpopulated</c>).
///  • Multiple rows with the same Event are <b>variants</b> — the game picks one (weighted, avoiding an
///    immediate repeat) so Alucia doesn't sound repetitive. Editors add a variant by copying a row.
///  • <b>Species</b> (optional) scopes a line to one fish; blank = any fish. Species-specific rows are
///    preferred over generic ones when a species is supplied.
///  • <b>Mood</b> (Calm/Warn/Win) tints her bubble. <b>Weight</b> biases random selection (blank = 1).
///  • Rows whose Event starts with <c>#</c> are comments and are ignored.
///
/// The file is resolved via <see cref="ContentService.LocalPathFor"/> (fresh online cache → baked copy),
/// and every caller passes the original hardcoded line as a fallback, so a missing/broken file or key
/// never breaks the game.
/// </summary>
public static class AluciaLines
{
    /// <summary>One resolved line: its text and mood, plus whether a CSV row was actually found.</summary>
    public struct Line
    {
        public string Text;
        public string Mood;  // "Calm" / "Warn" / "Win"
        public bool Found;
    }

    private struct Variant
    {
        public string Species;
        public string Mood;
        public int Weight;
        public string Text;
    }

    private const string FileName = "alucia_lines.csv";

    private static Dictionary<string, List<Variant>> _events;                 // eventKey (lower) -> variants
    private static readonly Dictionary<string, string> _lastShown = new Dictionary<string, string>(); // anti-repeat

    /// <summary>
    /// Backward-compatible accessor: returns the text for <paramref name="key"/> (an Event name),
    /// or <paramref name="fallback"/> if the file/event is missing. Picks a variant when several exist.
    /// </summary>
    public static string Get(string key, string fallback)
    {
        Line l = GetLine(key, null);
        return l.Found && !string.IsNullOrEmpty(l.Text) ? l.Text : fallback;
    }

    /// <summary>
    /// Resolves an event to a line (text + mood). When <paramref name="species"/> is given, a row scoped
    /// to that species wins over a generic one. <c>Found == false</c> means the caller should use its own
    /// hardcoded fallback. Tokens like <c>{species}</c>/<c>{count}</c>/<c>{req}</c> are left in the text
    /// for the caller to substitute.
    /// </summary>
    public static Line GetLine(string eventKey, string species)
    {
        EnsureLoaded();
        Line result = default;
        if (_events == null || string.IsNullOrEmpty(eventKey)) return result;
        if (!_events.TryGetValue(eventKey.Trim().ToLowerInvariant(), out List<Variant> variants) || variants.Count == 0)
            return result;

        List<Variant> pool = variants;
        if (!string.IsNullOrEmpty(species))
        {
            List<Variant> scoped = null, generic = null;
            for (int i = 0; i < variants.Count; i++)
            {
                if (string.Equals(variants[i].Species, species, System.StringComparison.OrdinalIgnoreCase))
                {
                    if (scoped == null) scoped = new List<Variant>();
                    scoped.Add(variants[i]);
                }
                else if (string.IsNullOrEmpty(variants[i].Species))
                {
                    if (generic == null) generic = new List<Variant>();
                    generic.Add(variants[i]);
                }
            }
            pool = scoped ?? generic ?? variants;
        }

        Variant v = Pick(pool, eventKey.ToLowerInvariant() + "|" + (species ?? ""));
        result.Text = v.Text;
        result.Mood = string.IsNullOrEmpty(v.Mood) ? "Calm" : v.Mood;
        result.Found = true;
        return result;
    }

    // Weighted random selection that avoids repeating the last line shown for this event+species.
    private static Variant Pick(List<Variant> pool, string memoryKey)
    {
        if (pool.Count == 1)
        {
            _lastShown[memoryKey] = pool[0].Text;
            return pool[0];
        }

        int total = 0;
        for (int i = 0; i < pool.Count; i++) total += Mathf.Max(1, pool[i].Weight);

        _lastShown.TryGetValue(memoryKey, out string last);
        Variant chosen = pool[0];
        for (int attempt = 0; attempt < 4; attempt++)
        {
            int roll = Random.Range(0, total);
            int acc = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                acc += Mathf.Max(1, pool[i].Weight);
                if (roll < acc) { chosen = pool[i]; break; }
            }
            if (chosen.Text != last) break; // got a non-repeat — good
        }
        _lastShown[memoryKey] = chosen.Text;
        return chosen;
    }

    /// <summary>Force a re-read (called after a live download, or from a debug hotkey while iterating).</summary>
    public static void Reload() { _events = null; _lastShown.Clear(); EnsureLoaded(); }

    private static void EnsureLoaded()
    {
        if (_events != null) return;
        _events = new Dictionary<string, List<Variant>>();

        string path = ContentService.LocalPathFor(FileName);
        try
        {
            if (!File.Exists(path))
            {
                Debug.Log($"[AluciaLines] No CSV at '{path}' — using built-in lines.");
                return;
            }
            Parse(File.ReadAllText(path));
            Debug.Log($"[AluciaLines] Loaded {_events.Count} event(s) from '{path}'.");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[AluciaLines] Could not read '{path}': {e.Message}. Using built-in lines.");
        }
    }

    private static void Parse(string text)
    {
        List<List<string>> rows = CsvUtil.Parse(text);
        if (rows.Count < 2) return;

        List<string> header = rows[0];
        int cEvent   = CsvUtil.ColumnIndex(header, "Event");
        int cSpecies = CsvUtil.ColumnIndex(header, "Species");
        int cMood    = CsvUtil.ColumnIndex(header, "Mood");
        int cWeight  = CsvUtil.ColumnIndex(header, "Weight");
        int cText    = CsvUtil.ColumnIndex(header, "Text");

        if (cEvent < 0 || cText < 0)
        {
            Debug.LogWarning("[AluciaLines] CSV must have 'Event' and 'Text' columns — ignoring file.");
            return;
        }

        for (int i = 1; i < rows.Count; i++)
        {
            List<string> row = rows[i];
            string evt = CsvUtil.Cell(row, cEvent).Trim();
            if (evt.Length == 0 || evt.StartsWith("#")) continue;   // blank or comment row
            string body = CsvUtil.Cell(row, cText);
            if (string.IsNullOrWhiteSpace(body)) continue;

            Variant v = new Variant
            {
                Species = CsvUtil.Cell(row, cSpecies).Trim(),
                Mood    = CsvUtil.Cell(row, cMood).Trim(),
                Weight  = ParseWeight(CsvUtil.Cell(row, cWeight)),
                Text    = body
            };

            string key = evt.ToLowerInvariant();
            if (!_events.TryGetValue(key, out List<Variant> list))
            {
                list = new List<Variant>();
                _events[key] = list;
            }
            list.Add(v);
        }
    }

    private static int ParseWeight(string s)
    {
        return int.TryParse((s ?? "").Trim(), out int w) && w > 0 ? w : 1;
    }
}
