using System;

namespace ChurchSigns.UI.Models
{
    /// <summary>
    /// Describes a print/PDF page and the pixel size to use when rasterizing a sign.
    ///
    /// Unit cheat sheet:
    ///   • Inches — physical paper (Letter = 8.5 × 11).
    ///   • Points (pt) — PDF / Skia page units.  72 pt = 1 inch.
    ///     SKDocument.BeginPage(widthPt, heightPt) uses points.
    ///   • Pixels — bitmap samples for SkiaSharp SVG render.
    ///     pixels ≈ inches × DPI  (300 DPI is a solid print default).
    ///
    /// Typical flow:
    ///   1) Rasterize SVG → SKBitmap sized PixelWidth × PixelHeight
    ///   2) BeginPage(PageWidthPt, PageHeightPt)
    ///   3) Draw bitmap into the content box (MarginPt inset, ContentWidthPt × ContentHeightPt)
    /// </summary>
    public sealed class PrintContentSize
    {
        /// <param name="pageWidthInches">Full page width (e.g. 8.5 portrait Letter, 11 landscape).</param>
        /// <param name="pageHeightInches">Full page height (e.g. 11 portrait Letter, 8.5 landscape).</param>
        /// <param name="marginInches">Blank border on each side (default 0.25").</param>
        /// <param name="dpi">
        /// Pixels per inch for the rasterized sign. 72 ≈ “1 pixel per PDF point” (soft when printed).
        /// 300 is appropriate for laser/inkjet; 150 is a lighter compromise.
        /// </param>
        public PrintContentSize(
            float pageWidthInches,
            float pageHeightInches,
            float marginInches = 0.25f,
            float dpi = 300f)
        {
            if (pageWidthInches <= 0 || pageHeightInches <= 0)
                throw new ArgumentOutOfRangeException(nameof(pageWidthInches));
            if (marginInches < 0)
                throw new ArgumentOutOfRangeException(nameof(marginInches));
            if (dpi <= 0)
                throw new ArgumentOutOfRangeException(nameof(dpi));

            // Printable area in inches (page minus left+right / top+bottom margins).
            var contentWidthInches = pageWidthInches - 2 * marginInches;
            var contentHeightInches = pageHeightInches - 2 * marginInches;
            if (contentWidthInches <= 0 || contentHeightInches <= 0)
                throw new ArgumentException("Margins leave no printable area.");

            PageWidthInches = pageWidthInches;
            PageHeightInches = pageHeightInches;
            MarginInches = marginInches;
            Dpi = dpi;

            // Bitmap size: physical content size × resolution.
            // Example: 10.5" × 300 DPI ≈ 3150 pixels wide (not 756 — that would be 72 DPI).
            PixelWidth = Math.Max(1, (int)Math.Round(contentWidthInches * dpi));
            PixelHeight = Math.Max(1, (int)Math.Round(contentHeightInches * dpi));
        }

        // ─── Physical page (inches) ───────────────────────────────────

        public float PageWidthInches { get; }
        public float PageHeightInches { get; }

        /// <summary>Inset on each edge, in inches.</summary>
        public float MarginInches { get; }

        /// <summary>Raster resolution used for <see cref="PixelWidth"/> / <see cref="PixelHeight"/>.</summary>
        public float Dpi { get; }

        // ─── Bitmap (pixels) — use when calling SKSvg / RenderPrintSizeBitmap ─

        /// <summary>
        /// Target bitmap width. Must be used for PDF/print renders so the image
        /// is not upscaled onto the page (which looks blurry).
        /// </summary>
        public int PixelWidth { get; }

        /// <summary>Target bitmap height for PDF/print renders.</summary>
        public int PixelHeight { get; }

        // ─── PDF / Skia page (points: 72 pt = 1 in) ───────────────────

        /// <summary>Full page width for <c>SKDocument.BeginPage</c>.</summary>
        public float PageWidthPt => PageWidthInches * 72f;

        /// <summary>Full page height for <c>SKDocument.BeginPage</c>.</summary>
        public float PageHeightPt => PageHeightInches * 72f;

        /// <summary>Left/top inset of the content box, in points.</summary>
        public float MarginPt => MarginInches * 72f;

        /// <summary>Width of the rectangle that should contain the sign bitmap.</summary>
        public float ContentWidthPt => (PageWidthInches - 2 * MarginInches) * 72f;

        /// <summary>Height of the rectangle that should contain the sign bitmap.</summary>
        public float ContentHeightPt => (PageHeightInches - 2 * MarginInches) * 72f;
    }
}