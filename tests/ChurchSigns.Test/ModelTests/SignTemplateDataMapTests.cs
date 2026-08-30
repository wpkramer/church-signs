using ChurchSigns.UI.Helpers;
using ChurchSigns.UI.Models;

namespace ChurchSigns.Test.ModelTests
{
    public class SignTemplateDataMapTests
    {
        private static SignTemplate CreateTemplate(params string[] fieldNames)
        {
            // Build minimal SVG so XmlDocument load succeeds and root is <svg>
            var placeholders = string.Join(
                "",
                fieldNames.Select(f => $"<text>{{{{{f}}}}}</text>"));

            var svg =
                $"""
        <svg xmlns="http://www.w3.org/2000/svg" width="100" height="100">
          {placeholders}
        </svg>
        """;

            var storage = new TemplateStorageItem
            {
                IsProvided = true, // or IsContent — match your property name
                SignCategory = SignCategory.Miscellaneous,
                Filename = "UnitTest.svg",
                Content = svg
                // FieldNames: if computed from Content, no need to set;
                // if settable, assign fieldNames here
            };

            var template = new SignTemplate(storage);
            Assert.True(template.IsValid);
            return template;
        }

        private static PastedRecordData CreatePaste(string headerLine, params string[] dataLines)
        {
            var text = headerLine + "\n" + string.Join("\n", dataLines);
            return new PastedRecordData(text);
        }

        //[Fact]
        //public void Constructor_DefaultsAllColumnsToNone()
        //{
        //    var template = CreateTemplate("Name", "Room");
        //    var data = CreatePaste("Name\tRoom", "Alice\t101");

        //    var map = new SignTemplateDataMap(template, data);

        //    // Force no auto-match yet: assign nothing; read after marking calculated via None assign
        //    // Or: first FieldIndex read runs auto-match — for defaults before match, use fresh map
        //    // and check via internal behavior after PerformFieldMatching with no good scores.
        //    // Safer: after construction, auto-match will map Name/Room — so test unmapped headers instead.
        //}

        [Fact]
        public void AutoMatch_ExactHeaders_MapsCorrectly()
        {
            var template = CreateTemplate("Name", "Room");
            var data = CreatePaste("Name\tRoom", "Alice\t101", "Bob\t102");

            var map = new SignTemplateDataMap(template, data);

            Assert.Equal(0, map.FieldIndexForDataColumn(0)); // Name
            Assert.Equal(1, map.FieldIndexForDataColumn(1)); // Room
            Assert.Equal(1, map.GetDropdownIndexForColumn(0)); // dropdown: (None)=0, Name=1
            Assert.Equal(2, map.GetDropdownIndexForColumn(1)); // Room=2
        }

        [Fact]
        public void AutoMatch_NormalizedHeaders_MapsLastName()
        {
            var template = CreateTemplate("Last Name");
            var data = CreatePaste("LastName", "Smith");

            var map = new SignTemplateDataMap(template, data);

            Assert.Equal(0, map.FieldIndexForDataColumn(0));
        }

        [Fact]
        public void AutoMatch_UnrelatedHeader_StaysNone()
        {
            var template = CreateTemplate("Name");
            var data = CreatePaste("FavoriteColor", "Blue");

            var map = new SignTemplateDataMap(template, data);

            Assert.Equal(-1, map.FieldIndexForDataColumn(0));
            Assert.Equal(0, map.GetDropdownIndexForColumn(0)); // (None)
        }

        [Fact]
        public void AutoMatch_ExtraColumns_UnmatchedStayNone()
        {
            var template = CreateTemplate("Name");
            var data = CreatePaste("Name\tNotes", "Alice\tHello");

            var map = new SignTemplateDataMap(template, data);

            Assert.Equal(0, map.FieldIndexForDataColumn(0));
            Assert.Equal(-1, map.FieldIndexForDataColumn(1));
        }

        [Fact]
        public void SetDropdownIndexForColumn_AssignsField()
        {
            var template = CreateTemplate("Name", "Room");
            var data = CreatePaste("ColA\tColB", "Alice\t101");

            var map = new SignTemplateDataMap(template, data);

            // Manual map ColA -> Name (dropdown index 1)
            int cleared = map.SetDropdownIndexForColumn(0, 1);

            Assert.Equal(-1, cleared);
            Assert.Equal(0, map.FieldIndexForDataColumn(0));
            Assert.Equal(1, map.GetDropdownIndexForColumn(0));
        }

        [Fact]
        public void SetDropdownIndexForColumn_MovingField_ClearsPreviousColumn()
        {
            var template = CreateTemplate("Name", "Room");
            var data = CreatePaste("ColA\tColB", "Alice\t101");

            var map = new SignTemplateDataMap(template, data);

            map.SetDropdownIndexForColumn(0, 1); // ColA -> Name
            int cleared = map.SetDropdownIndexForColumn(1, 1); // ColB -> Name (steal)

            Assert.Equal(0, cleared); // ColA reset to None
            Assert.Equal(-1, map.FieldIndexForDataColumn(0));
            Assert.Equal(0, map.FieldIndexForDataColumn(1));
        }

        [Fact]
        public void SetDropdownIndexForColumn_None_ClearsColumn_ReturnsMinusOne()
        {
            var template = CreateTemplate("Name");
            var data = CreatePaste("Name", "Alice");

            var map = new SignTemplateDataMap(template, data);
            _ = map.FieldIndexForDataColumn(0); // auto-match Name

            int cleared = map.SetDropdownIndexForColumn(0, 0); // (None)

            Assert.Equal(-1, cleared);
            Assert.Equal(-1, map.FieldIndexForDataColumn(0));
        }

        [Fact]
        public void AssignFieldIndexToDataColumn_InvalidField_Throws()
        {
            var template = CreateTemplate("Name");
            var data = CreatePaste("Name", "Alice");
            var map = new SignTemplateDataMap(template, data);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                map.AssignFieldIndexToDataColumn(5, 0));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                map.AssignFieldIndexToDataColumn(-2, 0));
        }

        [Fact]
        public void AssignFieldIndexToDataColumn_InvalidColumn_Throws()
        {
            var template = CreateTemplate("Name");
            var data = CreatePaste("Name", "Alice");
            var map = new SignTemplateDataMap(template, data);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                map.AssignFieldIndexToDataColumn(0, 3));
        }

        [Fact]
        public void DropdownFieldNames_StartsWithNone()
        {
            var template = CreateTemplate("Name", "Room");
            var data = CreatePaste("Name\tRoom", "Alice\t101");
            var map = new SignTemplateDataMap(template, data);

            Assert.Equal("(None)", map.DropdownFieldNames[0]);
            Assert.Equal("Name", map.DropdownFieldNames[1]);
            Assert.Equal("Room", map.DropdownFieldNames[2]);
            //Assert.Equal(3, map.DropdownFieldNames.Count);
        }

        [Fact]
        public void CreateMappedRecords_UsesMappedColumnsOnly()
        {
            var template = CreateTemplate("Name", "Room");
            var data = CreatePaste("Name\tNotes\tRoom", "Alice\tHi\t101", "Bob\tYo\t102");

            var map = new SignTemplateDataMap(template, data);
            // Auto: Name + Room; Notes stays None

            var rows = map.CreateMappedRecords().ToList();

            Assert.Equal(2, rows.Count);
            Assert.Equal("Alice", rows[0]["Name"]);
            Assert.Equal("101", rows[0]["Room"]);
            Assert.False(rows[0].ContainsKey("Notes"));
            Assert.Equal("Bob", rows[1]["Name"]);
            Assert.Equal("102", rows[1]["Room"]);
        }

        [Fact]
        public void CreateMappedRecords_AfterManualRemap_ReflectsChange()
        {
            var template = CreateTemplate("Name");
            var data = CreatePaste("FullName", "Alice");

            var map = new SignTemplateDataMap(template, data);
            map.SetDropdownIndexForColumn(0, 1); // FullName -> Name

            var row = map.CreateMappedRecords().Single();
            Assert.Equal("Alice", row["Name"]);
        }

        [Fact]
        public void FieldIndexForDataColumn_OutOfRange_ReturnsNone()
        {
            var template = CreateTemplate("Name");
            var data = CreatePaste("Name", "Alice");
            var map = new SignTemplateDataMap(template, data);

            Assert.Equal(-1, map.FieldIndexForDataColumn(-1));
            Assert.Equal(-1, map.FieldIndexForDataColumn(99));
        }

        [Fact]
        public void ManualAssign_PreventsLaterAutoMatchFromRunning()
        {
            var template = CreateTemplate("Name", "Room");
            var data = CreatePaste("Name\tRoom", "Alice\t101");

            var map = new SignTemplateDataMap(template, data);

            // User maps both to None before any read
            map.SetDropdownIndexForColumn(0, 0);
            map.SetDropdownIndexForColumn(1, 0);

            Assert.Equal(-1, map.FieldIndexForDataColumn(0));
            Assert.Equal(-1, map.FieldIndexForDataColumn(1));
        }
    }
}
