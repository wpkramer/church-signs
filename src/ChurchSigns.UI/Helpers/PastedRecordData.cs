using System;
using System.Collections.Generic;
using System.Linq;

namespace ChurchSigns.UI.Helpers
{
    public class PastedRecordData
    {
        public IReadOnlyList<string> ColumnHeaderNames { get; }
        public IReadOnlyList<IReadOnlyList<string>> Records { get; }

        public PastedRecordData(string pastedData)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pastedData);

            // Normalize Windows line endings and trim trailing blank lines
            var normalized = pastedData.Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd();
            var lines = normalized.Split('\n', StringSplitOptions.None)
                .Where(l => l.Length > 0)
                .ToArray();

            if (lines.Length < 2)
            {
                throw new ArgumentException(
                    "Pasted data requires a header row and at least one data row.",
                    nameof(pastedData));
            }

            var headers = lines[0].Split('\t');
            var rows = new List<IReadOnlyList<string>>(lines.Length - 1);

            for (int i = 1; i < lines.Length; i++)
            {
                var cells = lines[i].Split('\t');

                if (cells.Length != headers.Length)
                {
                    throw new ArgumentException(
                        $"Row {i} has {cells.Length} column(s), expected {headers.Length}.",
                        nameof(pastedData));
                }

                rows.Add(cells);
            }

            ColumnHeaderNames = headers;
            Records = rows;
        }

        /// <summary>
        /// Convenience: one dictionary per row (header → cell).
        /// </summary>
        public IEnumerable<Dictionary<string, string>> AsDictionaries()
        {
            foreach (var row in Records)
            {
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int c = 0; c < ColumnHeaderNames.Count; c++)
                    dict[ColumnHeaderNames[c]] = row[c];
                yield return dict;
            }
        }
    }
}