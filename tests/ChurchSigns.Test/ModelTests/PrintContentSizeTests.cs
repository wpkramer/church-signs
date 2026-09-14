using ChurchSigns.UI.Models;
using Xunit;

namespace ChurchSigns.Test.ModelTests
{
    public class PrintContentSizeTests
    {
        private const float LetterW = 8.5f;
        private const float LetterH = 11f;
        private const float DefaultMargin = 0.25f;

        [Fact]
        public void LetterPortrait_DefaultMargin_ExpectedPixelsAndPoints()
        {
            // Content: 8.0 × 10.5 in @ 300 DPI → 2400 × 3150 px
            var size = new PrintContentSize(LetterW, LetterH, DefaultMargin);

            Assert.Equal(LetterW, size.PageWidthInches);
            Assert.Equal(LetterH, size.PageHeightInches);
            Assert.Equal(DefaultMargin, size.MarginInches);

            Assert.Equal(2400, size.PixelWidth);
            Assert.Equal(3150, size.PixelHeight);

            Assert.Equal(8.5f * 72f, size.PageWidthPt);
            Assert.Equal(11f * 72f, size.PageHeightPt);
            Assert.Equal(0.25f * 72f, size.MarginPt);
            Assert.Equal(8.0f * 72f, size.ContentWidthPt);
            Assert.Equal(10.5f * 72f, size.ContentHeightPt);
        }

        [Fact]
        public void LetterLandscape_DefaultMargin_SwappedContentPixels()
        {
            // 11 × 8.5 page → content 10.5 × 8.0 in @ 300 → 3150 × 2400
            var size = new PrintContentSize(11f, 8.5f, DefaultMargin);

            Assert.Equal(3150, size.PixelWidth);
            Assert.Equal(2400, size.PixelHeight);
            Assert.Equal(11f * 72f, size.PageWidthPt);
            Assert.Equal(8.5f * 72f, size.PageHeightPt);
        }

        [Fact]
        public void PageDips_Use96PerInch()
        {
            var size = new PrintContentSize(LetterW, LetterH);

            Assert.Equal(8.5 * 96.0, size.PageWidthDips, precision: 5);
            Assert.Equal(11.0 * 96.0, size.PageHeightDips, precision: 5);
        }

        [Fact]
        public void ZeroMargin_ContentEqualsFullPage()
        {
            var size = new PrintContentSize(LetterW, LetterH, marginInches: 0f);

            Assert.Equal((int)Math.Round(8.5 * 300), size.PixelWidth);
            Assert.Equal((int)Math.Round(11.0 * 300), size.PixelHeight);
            Assert.Equal(0f, size.MarginPt);
            Assert.Equal(size.PageWidthPt, size.ContentWidthPt);
            Assert.Equal(size.PageHeightPt, size.ContentHeightPt);
        }

        [Fact]
        public void Constructor_ZeroPageWidth_Throws()
        {
            Assert.ThrowsAny<ArgumentException>(() =>
                new PrintContentSize(0f, LetterH));
        }

        [Fact]
        public void Constructor_NegativePageHeight_Throws()
        {
            Assert.ThrowsAny<ArgumentException>(() =>
                new PrintContentSize(LetterW, -1f));
        }

        [Fact]
        public void Constructor_NegativeMargin_Throws()
        {
            Assert.ThrowsAny<ArgumentException>(() =>
                new PrintContentSize(LetterW, LetterH, marginInches: -0.1f));
        }

        [Fact]
        public void Constructor_MarginConsumesEntirePage_Throws()
        {
            // 2 * 5 > 8.5
            Assert.Throws<ArgumentException>(() =>
                new PrintContentSize(LetterW, LetterH, marginInches: 5f));
        }

        [Fact]
        public void PerInch_InvalidPrintPixels_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PrintContentSize.PerInch(printPixels: 0f));
        }

        [Fact]
        public void PerInch_Defaults_300_96_72()
        {
            var per = new PrintContentSize.PerInch();
            Assert.Equal(300f, per.PrintPixels);
            Assert.Equal(96f, per.Dips);
            Assert.Equal(72f, per.PdfPoints);
        }

        [Fact]
        public void PixelSize_UsesRounding()
        {
            // Odd inches so Round is observable if factor changes later
            var size = new PrintContentSize(8.5f, 11f, marginInches: 0.125f);
            float contentW = 8.5f - 2 * 0.125f; // 8.25
            float contentH = 11f - 2 * 0.125f;  // 10.75

            Assert.Equal((int)Math.Round(contentW * 300f), size.PixelWidth);
            Assert.Equal((int)Math.Round(contentH * 300f), size.PixelHeight);
        }
    }
}