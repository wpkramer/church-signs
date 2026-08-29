using ChurchSigns.UI.Util;

namespace ChurchSigns.Test.UtilTests
{
    public class MatchConfidenceTests
    {
        [Fact]
        public void Score_ExactMatch_Is100()
        {
            var m = new MatchConfidence("Name", "Name");
            Assert.Equal(100, m.Score);
        }

        [Fact]
        public void Score_ExactMatch_IgnoresCaseAndTrim()
        {
            var m = new MatchConfidence("  Name  ", "name");
            Assert.Equal(100, m.Score);
        }

        [Fact]
        public void Score_NormalizedMatch_SpacesAndUnderscores_Is90()
        {
            var m = new MatchConfidence("Last Name", "LastName");
            Assert.Equal(90, m.Score);
        }

        [Fact]
        public void Score_NormalizedMatch_HyphenAndCase_Is90()
        {
            var m = new MatchConfidence("Teacher-Name", "teacher name");
            Assert.Equal(90, m.Score);
        }

        [Fact]
        public void Score_Containment_NameVsTeacherName_IsBetween0And70()
        {
            var m = new MatchConfidence("Name", "Teacher Name");
            Assert.InRange(m.Score, 1, 70);
            Assert.True(m.Score < 90);
        }

        [Fact]
        public void Score_UnrelatedStrings_Is0()
        {
            var m = new MatchConfidence("Name", "RoomNumber");
            Assert.Equal(0, m.Score);
        }

        [Fact]
        public void Score_EmptyOrWhitespace_Is0()
        {
            Assert.Equal(0, new MatchConfidence("Name", "").Score);
            Assert.Equal(0, new MatchConfidence("Name", "   ").Score);
            Assert.Equal(0, new MatchConfidence("", "Name").Score);
            Assert.Equal(0, new MatchConfidence("  ", "Name").Score);
        }

        [Fact]
        public void Score_DefaultConstructor_IsMinValue()
        {
            var m = new MatchConfidence();
            Assert.Equal(double.MinValue, m.Score);
        }

        [Fact]
        public void Score_CloseTypo_IsWeakMatchOrZero()
        {
            // "Teacher" vs "Techer" — may land in 40–60 band or 0 depending on length
            var m = new MatchConfidence("Teacher", "Techer");
            Assert.True(m.Score == 0 || (m.Score >= 40 && m.Score <= 60));
        }

        [Fact]
        public void CompareTo_HigherScore_IsGreater()
        {
            var exact = new MatchConfidence("Name", "Name");      // 100
            var normalized = new MatchConfidence("Last Name", "LastName"); // 90

            Assert.True(exact > normalized);
            Assert.True(normalized < exact);
            Assert.Equal(1, exact.CompareTo(normalized));
            Assert.Equal(-1, normalized.CompareTo(exact));
        }

        [Fact]
        public void CompareTo_EqualScores_AreEqual()
        {
            var a = new MatchConfidence("Name", "Name");
            var b = new MatchConfidence("Room", "Room");

            Assert.Equal(0, a.CompareTo(b));
            Assert.True(a >= b);
            Assert.True(a <= b);
        }

        [Fact]
        public void CompareTo_Null_ReturnsPositive()
        {
            var m = new MatchConfidence("Name", "Name");
            Assert.Equal(1, m.CompareTo((MatchConfidence?)null));
        }

        [Fact]
        public void Operators_WithNull_Work()
        {
            MatchConfidence? left = null;
            var right = new MatchConfidence("Name", "Name");

            Assert.True(left < right);
            Assert.True(left <= right);
            Assert.False(left > right);
            Assert.False(left >= right);
        }

        [Fact]
        public void Properties_ExposeConstructorArguments()
        {
            var m = new MatchConfidence("Last Name", "LastName");
            Assert.Equal("Last Name", m.CompareWith);
            Assert.Equal("LastName", m.TestString);
        }

        [Fact]
        public void Score_IsCached_SameValueOnRepeatedAccess()
        {
            var m = new MatchConfidence("Name", "name");
            var first = m.Score;
            var second = m.Score;
            Assert.Equal(first, second);
            Assert.Equal(100, first);
        }

        [Theory]
        [InlineData("Name", "Name", 100)]
        [InlineData("name", "NAME", 100)]
        [InlineData("Last Name", "LastName", 90)]
        [InlineData("Room_No", "RoomNo", 90)]
        [InlineData("Alpha", "Zeta", 0)]
        public void Score_TheoryCases(string field, string header, double expected)
        {
            var m = new MatchConfidence(field, header);
            Assert.Equal(expected, m.Score);
        }
    }
}