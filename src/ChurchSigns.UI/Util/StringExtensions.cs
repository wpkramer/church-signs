
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ChurchSigns.UI.Util
{
    public static class StringExtensions
    {
        private static readonly Regex FieldReplaceRegex = new(
            @"\{\{\s*([A-Za-z_][A-Za-z0-9_ ]*)\s*\}\}",
            RegexOptions.Compiled);

        private static readonly Regex FieldRegex = new(
        @"\{\{\s*([A-Za-z_][A-Za-z0-9_ ]*)\s*\}\}",
        RegexOptions.Compiled);

        public static string MergeTemplateWithData(this string template, IDictionary<string, string>? data)
        {
            if (string.IsNullOrEmpty(template) || data is null || data.Count == 0)
                return template;
            return FieldReplaceRegex.Replace(template, m =>
            {
                var key = m.Groups[1].Value;
                return data.TryGetValue(key, out var value) ? value : m.Value;
            });
        }

        /// <summary>
        /// Extracts unique field names from {{FieldName}} placeholders.
        /// </summary>
        public static IReadOnlyList<string> ExtractFieldNames(this string template)
        {
            if (string.IsNullOrEmpty(template))
                return Array.Empty<string>();

            return FieldRegex.Matches(template)
                .Select(m => m.Groups[1].Value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
