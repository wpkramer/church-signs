using ChurchSigns.UI.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChurchSigns.Test.ModelTests
{
    public class TemplateStorageItemTests
    {
        private const string twoFields = @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 100 100"">
                                            <text>{{Name}}</text><text>{{Room}}</text>
                                           </svg>";
        private const string repeatedField = @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 100 100"">
                                            <text>{{Name}}</text><text>{{Room}}</text>
                                            <text>{{Name}}</text>
                                           </svg>";
        private const string noFields = @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 100 100"">
                                            <text>John Doe</text><text>Room 123</text>
                                            <text>John Doe</text>
                                           </svg>";

        private const string badField = @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 100 100"">
                                            <text>{Name}}</text><text>{{Room}}</text>
                                           </svg>";



        [Fact]
        public void TwoFieldTest()
        {
            var tsi = new TemplateStorageItem();
            tsi.Content = twoFields;
            Assert.Equal(2,tsi.FieldNames.Count);
            Assert.Contains("Name", tsi.FieldNames);
            Assert.Contains("Room", tsi.FieldNames);
        }

        [Fact]
        public void NoDuplicateFieldTest()
        {
            var tsi = new TemplateStorageItem();
            tsi.Content = repeatedField;
            Assert.Equal(2, tsi.FieldNames.Count);
            Assert.Contains("Name", tsi.FieldNames);
            Assert.Contains("Room", tsi.FieldNames);
        }

        [Fact]
        public void NoFieldsTest()
        {
            var tsi = new TemplateStorageItem();
            tsi.Content = noFields;
            Assert.Empty(tsi.FieldNames);
        }

        [Fact]
        public void BadFieldTest()
        {
            var tsi = new TemplateStorageItem();
            tsi.Content = badField;
            Assert.Single(tsi.FieldNames);
            Assert.Contains("Room", tsi.FieldNames);
        }

        [Fact]
        public void DefaultConstructor_EmptyFieldNames()
        {
            var tsi = new TemplateStorageItem();
            Assert.Empty(tsi.FieldNames);
        }

        [Fact]
        public void FieldNames_WhitespaceInsideBraces_Trimmed()
        {
            var tsi = new TemplateStorageItem
            {
                Content = """<svg xmlns="http://www.w3.org/2000/svg"><text>{{ Name }}</text></svg>"""
            };
            Assert.Single(tsi.FieldNames);
            Assert.Equal(["Name"], tsi.FieldNames);
        }

        [Fact]
        public void FieldNames_WhitespaceInsideNames()
        {
            var tsi = new TemplateStorageItem
            {
                Content = """<svg xmlns="http://www.w3.org/2000/svg"><text>{{ Last Name }}</text></svg>"""
            };
            Assert.Single(tsi.FieldNames);
            Assert.Equal(["Last Name"], tsi.FieldNames); 
        }

        [Fact]
        public void DisplayName_StripsExtension()
        {
            var tsi = new TemplateStorageItem { Filename = "LeaderSign.svg" };
            Assert.Equal("LeaderSign", tsi.DisplayName);
        }

        [Fact]
        public void SideCar_Default_NotNull()
        {
            var tsi = new TemplateStorageItem();
            Assert.NotNull(tsi.SideCar);
        }
    }
}
