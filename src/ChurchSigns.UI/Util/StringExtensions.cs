using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ChurchSigns.UI.Util
{
    public static class StringExtensions
    {
        private static readonly Regex FieldRegex = new(
        @"\{\{\s*([A-Za-z_][A-Za-z0-9_]*)\s*\}\}",
        RegexOptions.Compiled);

        /// <summary>
        /// Extracts unique field names from {{FieldName}} placeholders.
        /// </summary>
        public static IReadOnlyList<string> ExtractFieldNames(this string template)
        {
            if (string.IsNullOrEmpty(template))
                return Array.Empty<string>();

            return FieldRegex.Matches(template)
                .Select(m => m.Groups[1].Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
