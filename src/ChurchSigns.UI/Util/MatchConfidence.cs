// Can you generate a unit test class for matchconfidence?
using System;
using System.Collections.Generic;
using System.Text;

namespace ChurchSigns.UI.Util
{
    public class MatchConfidence : IComparable<MatchConfidence>
    {
        private double _score;
        private readonly string _compareWith;
        private readonly string _testString;

        public MatchConfidence()
        {
            _score = double.MinValue;
            _compareWith = string.Empty;
            _testString = string.Empty;
        }

        public MatchConfidence(string compareWith, string testString)
        {
            _compareWith = compareWith;
            _testString = testString;
            _score = -1d;
        }

        public override string ToString()
        {
            return $"({Score}) \"{CompareWith}\" \"{TestString}\" ";
        }

        public String CompareWith { get { return _compareWith; } }
        public string TestString { get { return _testString; } }

        public double Score
        {
            get
            {
                if(_score == -1d)
                {
                    _score = CalculateScore();
                }
                return _score;
            }
        }

        private double CalculateScore()
        {
            var a = Normalize(_compareWith);
            var b = Normalize(_testString);

            if (a.Length == 0 || b.Length == 0)
                return 0;

            // Exact (case-insensitive, trimmed) — use original trim for this tier
            if (string.Equals(_compareWith.Trim(), _testString.Trim(), StringComparison.OrdinalIgnoreCase))
                return 100;

            // Same after removing spaces / punctuation
            if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
                return 90;

            // Containment (e.g. "Name" vs "Teacher Name")
            if (a.Contains(b, StringComparison.OrdinalIgnoreCase) ||
                b.Contains(a, StringComparison.OrdinalIgnoreCase))
            {
                // Prefer when lengths are close
                var ratio = (double)Math.Min(a.Length, b.Length) / Math.Max(a.Length, b.Length);
                return 70 * ratio;
            }

            // Simple edit-distance similarity on normalized strings
            var distance = Levenshtein(a, b);
            var maxLen = Math.Max(a.Length, b.Length);
            var similarity = 1.0 - (double)distance / maxLen;

            // Only treat as a weak match if reasonably close
            if (similarity >= 0.75)
                return 40 + (similarity - 0.75) / 0.25 * 20; // 40–60

            return 0;
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var sb = new StringBuilder(value.Length);
            foreach (var ch in value.Trim())
            {
                if (char.IsLetterOrDigit(ch))
                    sb.Append(char.ToLowerInvariant(ch));
                // skip spaces, _, -, etc.
            }
            return sb.ToString();
        }

        private static int Levenshtein(string s, string t)
        {
            var n = s.Length;
            var m = t.Length;
            var d = new int[n + 1, m + 1];

            for (var i = 0; i <= n; i++) d[i, 0] = i;
            for (var j = 0; j <= m; j++) d[0, j] = j;

            for (var i = 1; i <= n; i++)
            {
                for (var j = 1; j <= m; j++)
                {
                    var cost = s[i - 1] == t[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }

        public static bool operator <(MatchConfidence left, MatchConfidence right)
        {
            return left is null ? right is not null : left.CompareTo(right) < 0;
        }

        public static bool operator <=(MatchConfidence left, MatchConfidence right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(MatchConfidence left, MatchConfidence right)
        {
            return left is not null && left.CompareTo(right) > 0;
        }

        public static bool operator >=(MatchConfidence left, MatchConfidence right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return -1;
            if (obj is MatchConfidence other)
            {
                return this.Score.CompareTo(other);
            }
            if (obj is double score)
            {
                return this.Score.CompareTo(score);
            }
            return -1;
        }

        public int CompareTo(MatchConfidence other)
        {
            if(other == null) return 1;
            return this.Score.CompareTo(other.Score);
        }
    }
}
