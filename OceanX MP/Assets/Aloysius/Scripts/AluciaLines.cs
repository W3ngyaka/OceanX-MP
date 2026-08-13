using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class AluciaLines
{

    public struct Line
    {
        public string Text;
        public string Mood;
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

    private static Dictionary<string, List<Variant>> _events;
    private static readonly Dictionary<string, string> _lastShown = new Dictionary<string, string>();

    public static string Get(string key, string fallback)
    {
        Line l = GetLine(key, null);
        return l.Found && !string.IsNullOrEmpty(l.Text) ? l.Text : fallback;
    }

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

    public static List<string> GetVariants(string eventKey, string species)
    {
        EnsureLoaded();
        var result = new List<string>();
        if (_events == null || string.IsNullOrEmpty(eventKey)) return result;
        if (!_events.TryGetValue(eventKey.Trim().ToLowerInvariant(), out List<Variant> variants) || variants.Count == 0)
            return result;

        List<Variant> scoped = null, generic = null;
        for (int i = 0; i < variants.Count; i++)
        {
            if (!string.IsNullOrEmpty(species) &&
                string.Equals(variants[i].Species, species, System.StringComparison.OrdinalIgnoreCase))
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

        List<Variant> pool = scoped ?? generic ?? variants;
        for (int i = 0; i < pool.Count; i++)
            if (!string.IsNullOrEmpty(pool[i].Text)) result.Add(pool[i].Text);
        return result;
    }

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
            if (chosen.Text != last) break;
        }
        _lastShown[memoryKey] = chosen.Text;
        return chosen;
    }

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
            if (evt.Length == 0 || evt.StartsWith("#")) continue;
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
