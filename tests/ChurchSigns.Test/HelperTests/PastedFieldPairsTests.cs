using ChurchSigns.UI.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChurchSigns.Test.HelperTests
{
    public class PastedFieldPairsTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("\r\n")]
        public void WhitespaceTest(string testData)
        {
            var pairs = new PastedFieldPairs(testData);
            Assert.Empty(pairs.Fields);
        }

        [Fact]
        public void TwoEntriesTest()
        {
            var pairs = new PastedFieldPairs("Name\tAlice\nRoom\t101");
            Assert.Equal(2,pairs.Fields.Count);
            Assert.Equal("Alice", pairs.Fields["Name"]);
            Assert.Equal("101", pairs.Fields["Room"]);
        }

        [Fact]
        public void DuplicateKeyTest()
        {
            var pairs = new PastedFieldPairs("Name\tAlice\nRoom\t101\nName\tJoe");
            // only first "Name"
            Assert.Equal("Alice", pairs.Fields["Name"]);
        }

        [Fact]
        public void DuplicateKeyIgnoreCaseTest()
        {
            var pairs = new PastedFieldPairs("Name\tAlice\nRoom\t101\nname\tJoe");
            // only first "Name"
            Assert.Equal("Alice", pairs.Fields["Name"]);
            Assert.Equal("Alice", pairs.Fields["name"]);
        }

        [Fact]
        public void SingleColumnSkippedTest()
        {
            var pairs = new PastedFieldPairs("Name\tAlice\nSection\nRoom\t101");
            // Section is skipped
            Assert.Equal(2, pairs.Fields.Count);
            Assert.Equal("Alice", pairs.Fields["Name"]);
            Assert.Equal("101", pairs.Fields["Room"]);

        }
    }
}
