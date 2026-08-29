// grok, can you update this xunit test class with tests
// that exercise the PastedRecordData class?
using ChurchSigns.UI.Helpers;


namespace ChurchSigns.Test.HelperTests
{
    public class PastedRecordDataTests
    {
        [Fact]
        public void Constructor_EmptyString_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new PastedRecordData(string.Empty));
        }

        [Fact]
        public void Constructor_Null_Throws()
        {
            Assert.ThrowsAny<ArgumentException>(() => new PastedRecordData(null!));
        }

        [Fact]
        public void Constructor_WhitespaceOnly_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new PastedRecordData("   \t\n  "));
        }

        [Fact]
        public void Constructor_HeaderOnly_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() => new PastedRecordData("Name\tRoom"));
            Assert.Contains("header", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Constructor_ValidSingleRow_ParsesHeadersAndRecord()
        {
            var data = new PastedRecordData("Name\tRoom\nAlice\t101");

            Assert.Equal(new[] { "Name", "Room" }, data.ColumnHeaderNames);
            Assert.Single(data.Records);
            Assert.Equal(new[] { "Alice", "101" }, data.Records[0]);
        }

        [Fact]
        public void Constructor_ValidMultipleRows_ParsesAllRecords()
        {
            var pasted =
                "Name\tRoom\n" +
                "Alice\t101\n" +
                "Bob\t102\n" +
                "Carol\t103";

            var data = new PastedRecordData(pasted);

            Assert.Equal(2, data.ColumnHeaderNames.Count);
            Assert.Equal(3, data.Records.Count);
            Assert.Equal("Bob", data.Records[1][0]);
            Assert.Equal("103", data.Records[2][1]);
        }

        [Fact]
        public void Constructor_WindowsLineEndings_ParsesCorrectly()
        {
            var pasted = "Name\tRoom\r\nAlice\t101\r\nBob\t102\r\n";

            var data = new PastedRecordData(pasted);

            Assert.Equal(new[] { "Name", "Room" }, data.ColumnHeaderNames);
            Assert.Equal(2, data.Records.Count);
            Assert.Equal("Alice", data.Records[0][0]);
            Assert.Equal("102", data.Records[1][1]);
        }

        [Fact]
        public void Constructor_TrailingNewline_IgnoresEmptyLine()
        {
            var pasted = "Name\tRoom\nAlice\t101\n";

            var data = new PastedRecordData(pasted);

            Assert.Single(data.Records);
            Assert.Equal("Alice", data.Records[0][0]);
        }

        [Fact]
        public void Constructor_ColumnCountMismatch_ThrowsArgumentException()
        {
            // Header has 2 columns; second data row has only 1
            var pasted = "Name\tRoom\nAlice\t101\nBob";

            var ex = Assert.Throws<ArgumentException>(() => new PastedRecordData(pasted));
            Assert.Contains("column", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Constructor_ExtraColumnOnRow_ThrowsArgumentException()
        {
            var pasted = "Name\tRoom\nAlice\t101\tExtra";

            Assert.Throws<ArgumentException>(() => new PastedRecordData(pasted));
        }

        [Fact]
        public void Constructor_SingleColumn_Parses()
        {
            var pasted = "Name\nAlice\nBob";

            var data = new PastedRecordData(pasted);

            Assert.Equal(new[] { "Name" }, data.ColumnHeaderNames);
            Assert.Equal(2, data.Records.Count);
            Assert.Equal("Alice", data.Records[0][0]);
            Assert.Equal("Bob", data.Records[1][0]);
        }

        [Fact]
        public void AsDictionaries_MapsHeadersToValues()
        {
            var data = new PastedRecordData("Name\tRoom\nAlice\t101\nBob\t102");

            var rows = data.AsDictionaries().ToList();

            Assert.Equal(2, rows.Count);
            Assert.Equal("Alice", rows[0]["Name"]);
            Assert.Equal("101", rows[0]["Room"]);
            Assert.Equal("Bob", rows[1]["Name"]);
            Assert.Equal("102", rows[1]["Room"]);
        }

        [Fact]
        public void AsDictionaries_HeaderLookup_IsCaseInsensitive()
        {
            var data = new PastedRecordData("Name\tRoom\nAlice\t101");

            var row = data.AsDictionaries().Single();

            Assert.Equal("Alice", row["name"]);
            Assert.Equal("101", row["ROOM"]);
        }
    }
}
