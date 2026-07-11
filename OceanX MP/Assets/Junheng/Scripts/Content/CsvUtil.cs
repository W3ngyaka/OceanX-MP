using System.Collections.Generic;
using System.Text;

/// <summary>
/// Shared, tolerant CSV parser used by every content loader (Alucia lines, species content,
/// and any future tablet-text sheet). RFC-4180-ish: honours quoted fields, embedded commas,
/// embedded newlines, and "" escapes, so a fact-checker can safely put line breaks and commas
/// inside a cell in Excel / Google Sheets without breaking the file.
/// </summary>
public static class CsvUtil
{
    /// <summary>Parses whole CSV text into rows of string fields. Strips a leading UTF-8 BOM.</summary>
    public static List<List<string>> Parse(string text)
    {
        var rows = new List<List<string>>();
        if (string.IsNullOrEmpty(text)) return rows;
        if (text[0] == '﻿') text = text.Substring(1); // Excel/Sheets often prepend a BOM

        var row = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"') { sb.Append('"'); i++; } // "" -> "
                    else inQuotes = false;
                }
                else sb.Append(c);
            }
            else
            {
                if (c == '"') inQuotes = true;
                else if (c == ',') { row.Add(sb.ToString()); sb.Clear(); }
                else if (c == '\r') { /* ignore; handled on \n */ }
                else if (c == '\n') { row.Add(sb.ToString()); sb.Clear(); rows.Add(row); row = new List<string>(); }
                else sb.Append(c);
            }
        }
        if (sb.Length > 0 || row.Count > 0) { row.Add(sb.ToString()); rows.Add(row); }

        // Drop fully-empty trailing rows (a blank last line, etc.).
        rows.RemoveAll(r => r.Count == 1 && string.IsNullOrEmpty(r[0]));
        return rows;
    }

    /// <summary>Case-insensitive header lookup. Returns the column index for <paramref name="name"/>, or -1.</summary>
    public static int ColumnIndex(List<string> header, string name)
    {
        if (header == null) return -1;
        for (int i = 0; i < header.Count; i++)
            if (string.Equals(header[i].Trim(), name, System.StringComparison.OrdinalIgnoreCase))
                return i;
        return -1;
    }

    /// <summary>Safe cell read: returns "" when the column is missing or out of range.</summary>
    public static string Cell(List<string> row, int index)
        => (row != null && index >= 0 && index < row.Count) ? row[index] : "";
}
