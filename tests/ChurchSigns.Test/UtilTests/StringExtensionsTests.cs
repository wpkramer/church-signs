using System;
using System.Collections.Generic;
using System.Text;
using ChurchSigns.UI;
using ChurchSigns.UI.Util;
namespace ChurchSigns.Test.UtilTests
{
    public class StringExtensionsTests
    {
        [Fact]
        public void TestSingleField()
        {
            string test = "this contains {{onefield}} in the text";
            var result = test.ExtractFieldNames();
            Assert.True(result.Count == 1) ;
        }
        [Fact]
        public void TestMultipleFields()
        {
            string test = "this contains {{one field}} and {{another field}} and yet {{field3}} in the text";
            var result = test.ExtractFieldNames();
            Assert.True(result.Count == 3);
        }
        [Fact]
        public void TestNoFields()
        {
            string test = "just a simple string";
            var result = test.ExtractFieldNames();
            Assert.True(result.Count == 0);
        }
        [Fact]
        public void TestReuseOfFields()
        {
            string test = "this contains {{one field}} and {{another field}} and again {{one field}} in the text";
            var result = test.ExtractFieldNames();
            Assert.True(result.Count == 2);
        }
    }
}
