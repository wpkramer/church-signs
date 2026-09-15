using System;
using System.Collections.Generic;
using System.Text;

namespace ChurchSigns.UI.Helpers
{
    public sealed class PastedFieldPairs
    {
        public IReadOnlyDictionary<string, string> Fields { get; }

        public PastedFieldPairs(string pastedText)
        {
           Fields = ParseFieldPairs(pastedText);
        }

        private static Dictionary<string,string> ParseFieldPairs(string text)
        {
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(text))
                return fields;

            string normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');

            foreach (var line in normalized.Split('\n'))
            {
                string trimmed = line.Trim();
                if (trimmed.Length == 0) continue;

                string[] parts = trimmed.Split('\t');

                // must have at least two tab delimited values
                if (parts.Length < 2)
                    continue;
                
                string key = parts[0].Trim();
                if(key.Length == 0) continue;

                // skip any other tab delimited values
                string fieldValue = parts[1].Trim(); 
                
                // test for column header "Name" "Value"
                if (fields.Count == 0 && IsHeader(key, fieldValue)) continue;

                // first key wins
                fields.TryAdd(key, fieldValue);
                
                
            }
            return fields;

        }

        static bool IsHeader(string a, string b) =>
            (a.Equals("Field", StringComparison.OrdinalIgnoreCase)
                && b.Equals("Value", StringComparison.OrdinalIgnoreCase))
            || (a.Equals("Name", StringComparison.OrdinalIgnoreCase)
                && b.Equals("Value", StringComparison.OrdinalIgnoreCase));

    }
}
