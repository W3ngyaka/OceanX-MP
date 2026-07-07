using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Loads Alucia's dialogue from <c>StreamingAssets/alucia_lines.csv</c> so that
/// non-developers (fact-checkers) can edit what she says in a spreadsheet without
/// ever opening Unity or the Inspector.
///
/// On the Windows HOST build the CSV ships as a loose, editable file at
/// <c>&lt;Build&gt;_Data/StreamingAssets/alucia_lines.csv</c> — edit the Text column,
/// save, relaunch the game, and the new lines appear. No rebuild needed.
///
/// Every caller passes the original hardcoded line as a <paramref name="fallback"/>,
/// so if the CSV is missing, unreadable, or a key is absent, the game still shows
/// sensible text and nothing breaks. Keys (first column) are stable IDs the checkers
/// should NOT change; they only edit the Text column.
///
/// NOTE (Android/tablet): StreamingAssets is packed inside the .apk there, so the
/// file is not directly readable/editable — those builds fall back to the built-in
/// lines. Alucia runs on the Windows host, which is the target for live editing.
/// </summary>
public static class AluciaLines
{
    private const string FileName = "alucia_lines.csv";
    private static Dictionary<string, string> _lines;

    /// <summary>
    /// Returns the CSV text for <paramref name="key"/>, or <paramref name="fallback"/>
    /// if the file/key is missing or its Text cell is empty.
    /// </summary>
    public static string Get(string key, string fallback)
    {
        EnsureLoaded();
        if (_lines != null && _lines.TryGetValue(key, out string v) && !string.IsNullOrEmpty(v))
            return v;
        return fallback;
    }

    /// <summary>Force a re-read from disk (handy for a debug hotkey while iterating).</summary>
    public static void Reload() { _lines = null; EnsureLoaded(); }

    private static void EnsureLoaded()
    {
        if (_lines != null) return;
        _lines = new Dictionary<string, string>();

        string path = Path.Combine(Application.streamingAssetsPath, FileName);
        try
        {
            if (!File.Exists(path))
            {
                Debug.Log($"[AluciaLines] No CSV at '{path}' — using built-in lines.");
                return;
            }
            Parse(File.ReadAllText(path));
            Debug.Log($"[AluciaLines] Loaded {_lines.Count} line(s) from '{path}'.");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[AluciaLines] Could not read '{path}': {e.Message}. Using built-in lines.");
        }
    }

    private static void Parse(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (text[0] == '﻿') text = text.Substring(1); // strip UTF-8 BOM (Excel adds one)

        string[] rows = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        if (rows.Length == 0) return;

        List<string> header = ParseLine(rows[0]);
        int keyCol = ColumnIndex(header, "Key");
        int textCol = ColumnIndex(header, "Text");
        if (keyCol < 0 || textCol < 0)
        {
            Debug.LogWarning("[AluciaLines] CSV must have 'Key' and 'Text' columns — ignoring file.");
            return;
        }

        for (int i = 1; i < rows.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(rows[i])) continue;
            List<string> cols = ParseLine(rows[i]);
            if (keyCol >= cols.Count || textCol >= cols.Count) continue;
            string key = cols[keyCol].Trim();
            if (key.Length > 0) _lines[key] = cols[textCol];
        }
    }

    private static int ColumnIndex(List<string> header, string name)
    {
        for (int i = 0; i < header.Count; i++)
            if (string.Equals(header[i].Trim(), name, System.StringComparison.OrdinalIgnoreCase))
                return i;
        return -1;
    }

    // Splits one CSV record into fields, honouring quoted fields so commas, quotes
    // ("" -> ") and spaces inside quotes are preserved. Assumes no newline inside a field.
    private static List<string> ParseLine(string line)
    {
        var fields = new List<string>();
        var sb = new System.Text.StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else inQuotes = false;
                }
                else sb.Append(c);
            }
            else
            {
                if (c == '"') inQuotes = true;
                else if (c == ',') { fields.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(c);
            }
        }
        fields.Add(sb.ToString());
        return fields;
    }
}
