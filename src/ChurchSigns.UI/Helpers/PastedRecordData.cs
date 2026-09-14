using System;
using System.Collections.Generic;
using System.Linq;

namespace ChurchSigns.UI.Helpers
{
    public class PastedRecordData
    {
        public IReadOnlyList<string> ColumnHeaderNames { get; }
        public IReadOnlyList<IReadOnlyList<string>> Records { get; }

  //      public static implicit operator string(PastedRecordData data) => data.ToString() ?? string.Empty;
        public static explicit operator PastedRecordData(string pastedData) => new PastedRecordData(pastedData);

        private readonly string _originalPaste;

        public PastedRecordData()
            : this(string.Empty)
        { }

        public PastedRecordData(string pastedData)
        {
             //   ArgumentException.ThrowIfNullOrWhiteSpace(pastedData);
            if(string.IsNullOrWhiteSpace(pastedData))
            {
                _originalPaste = string.Empty;
                ColumnHeaderNames = new List<string>();
                Records = new List<IReadOnlyList<string>>();
                return;
            }
            _originalPaste = pastedData;
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

        public override string ToString()
        {
            return _originalPaste;
        }

        public static bool operator ==(PastedRecordData? d1, PastedRecordData? d2)
        {
            if (ReferenceEquals(d1, d2)) return true;
            if (d1 is null || d2 is null) return false;
            return d1.Equals(d2);
        }

        public static bool operator !=(PastedRecordData? d1, PastedRecordData? d2) => !(d1 == d2);

        public override bool Equals(object? obj) =>
            obj is PastedRecordData other &&
            string.Equals(_originalPaste, other._originalPaste, StringComparison.Ordinal);

        public override int GetHashCode() => _originalPaste?.GetHashCode() ?? 0;



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