using ChurchSigns.UI.Helpers;
using ChurchSigns.UI.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChurchSigns.UI.Models
{
    /// <summary>
    /// Maps pasted spreadsheet columns to template fields for UI dropdowns.
    /// Each column maps to at most one field; each field maps to at most one column.
    /// Unmapped columns use field index -1 (UI shows as "(None)").
    /// </summary>
    public class SignTemplateDataMap
    {
        private const double AutoMatchMinimumScore = 70;

        private readonly SignTemplate _template;
        private readonly PastedRecordData _data;

        /// <summary>
        /// For each pasted column: template field index, or -1 if unused.
        /// </summary>
        private readonly int[] _columnToFieldMap;

        private bool _wasCalculated;

        public SignTemplateDataMap(SignTemplate template, PastedRecordData data)
        {
            ArgumentNullException.ThrowIfNull(template);
            ArgumentNullException.ThrowIfNull(data);

            _template = template;
            _data = data;
            _wasCalculated = false;

            // Default: all columns unmapped
            _columnToFieldMap = new int[data.ColumnHeaderNames.Count];
            Array.Fill(_columnToFieldMap, -1);
        }

        /// <summary>
        /// Dropdown labels: "(None)" then each template field name.
        /// </summary>
        public IReadOnlyList<string> DropdownFieldNames
        {
            get
            {
                var list = new List<string>(_template.FieldNames.Count + 1) { "(None)" };
                list.AddRange(_template.FieldNames);
                return list;
            }
        }

        public IReadOnlyList<string> ColumnHeaderNames => _data.ColumnHeaderNames;

        public IReadOnlyList<string> TemplateFieldNames => _template.FieldNames;

        /// <summary>
        /// Template field index for a pasted column, or -1 if none.
        /// Triggers auto-match on first read if the user has not assigned manually.
        /// </summary>
        public int FieldIndexForDataColumn(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= _columnToFieldMap.Length)
                return -1;

            if (!_wasCalculated)
                PerformFieldMatching();

            return _columnToFieldMap[columnIndex];
        }

        /// <summary>
        /// Dropdown selected index for a column: 0 = (None), 1..N = template fields.
        /// </summary>
        public int GetDropdownIndexForColumn(int columnIndex)
        {
            var fieldIndex = FieldIndexForDataColumn(columnIndex);
            return fieldIndex < 0 ? 0 : fieldIndex + 1;
        }

        /// <summary>
        /// Apply a dropdown selection (0 = None, 1..N = field).
        /// </summary>
        public int SetDropdownIndexForColumn(int columnIndex, int dropdownIndex)
        {
            var fieldIndex = dropdownIndex <= 0 ? -1 : dropdownIndex - 1;
            return AssignFieldIndexToDataColumn(fieldIndex, columnIndex);
        }
        /// <summary>
        /// Maps a template field to a data column.
        /// Returns the column index that was cleared to None because it previously
        /// held this field, or -1 if none.
        /// </summary>
        public int AssignFieldIndexToDataColumn(int fieldIndex, int columnIndex)
        {
            if (fieldIndex < -1 || fieldIndex >= _template.FieldNames.Count)
                throw new ArgumentOutOfRangeException(nameof(fieldIndex));

            if (columnIndex < 0 || columnIndex >= _columnToFieldMap.Length)
                throw new ArgumentOutOfRangeException(nameof(columnIndex));

            _wasCalculated = true;

            int clearedColumnIndex = -1;

            // This field may only appear on one column
            if (fieldIndex >= 0)
            {
                for (int i = 0; i < _columnToFieldMap.Length; i++)
                {
                    if (_columnToFieldMap[i] != fieldIndex)
                        continue;

                    // Same column: no "reset elsewhere"
                    if (i == columnIndex)
                        break;

                    _columnToFieldMap[i] = -1;
                    clearedColumnIndex = i;
                    break; // at most one column can hold this field
                }
            }

            _columnToFieldMap[columnIndex] = fieldIndex;
            return clearedColumnIndex;
        }

        /// <summary>
        /// Build one dictionary per data row using the current column→field map.
        /// Only mapped columns are included (keys = template field names).
        /// </summary>
        public IEnumerable<Dictionary<string, string>> CreateMappedRecords()
        {
            if (!_wasCalculated)
                PerformFieldMatching();

            foreach (var row in _data.Records)
            {
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                for (int col = 0; col < _columnToFieldMap.Length; col++)
                {
                    var fieldIndex = _columnToFieldMap[col];
                    if (fieldIndex < 0)
                        continue;

                    var fieldName = _template.FieldNames[fieldIndex];
                    var value = col < row.Count ? row[col] : string.Empty;
                    dict[fieldName] = value;
                }

                yield return dict;
            }
        }

        private void PerformFieldMatching()
        {
            _wasCalculated = true;
            Array.Fill(_columnToFieldMap, -1);

            var matches = new List<ColumnMatch>(_template.FieldNames.Count);

            for (int fieldIndex = 0; fieldIndex < _template.FieldNames.Count; fieldIndex++)
            {
                var match = BestHeaderForField(_template.FieldNames[fieldIndex], fieldIndex);
                if (match.ColumnIndex >= 0)
                    matches.Add(match);
            }

            // Highest confidence first
            matches.Sort((a, b) => b.Confidence.Score.CompareTo(a.Confidence.Score));

            foreach (var match in matches)
            {
                if (match.Confidence.Score < AutoMatchMinimumScore)
                    break;

                // Column already taken by a stronger match
                if (_columnToFieldMap[match.ColumnIndex] >= 0)
                    continue;

                // Field already used (should not happen with one entry per field)
                if (_columnToFieldMap.Contains(match.FieldIndex))
                    continue;

                _columnToFieldMap[match.ColumnIndex] = match.FieldIndex;
            }
        }

        private ColumnMatch BestHeaderForField(string fieldName, int fieldIndex)
        {
            MatchConfidence? best = null;
            int bestColumn = -1;

            for (int col = 0; col < _data.ColumnHeaderNames.Count; col++)
            {
                var candidate = new MatchConfidence(fieldName, _data.ColumnHeaderNames[col]);
                if (best is null || candidate > best)
                {
                    best = candidate;
                    bestColumn = col;
                }
            }

            return new ColumnMatch
            {
                FieldIndex = fieldIndex,
                ColumnIndex = bestColumn,
                Confidence = best ?? new MatchConfidence(fieldName, string.Empty)
            };
        }

        private sealed class ColumnMatch
        {
            public int FieldIndex { get; init; }
            public int ColumnIndex { get; init; }
            public MatchConfidence Confidence { get; init; } = null!;
        }
    }
}