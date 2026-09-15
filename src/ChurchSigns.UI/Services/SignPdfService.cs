using ChurchSigns.UI.Models;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
namespace ChurchSigns.UI.Services;

public static class SignPdfService
{
    public static async Task ExportSelectedSignsAsync(IReadOnlyList<ChurchSign> signs, StorageFile file)
    {
        ArgumentNullException.ThrowIfNull(signs);

        if (signs.Count == 0)
            return;


        if (file != null)
        {
            using var stream = await file.OpenStreamForWriteAsync();
            // Truncate if replacing an existing file
            stream.SetLength(0);

            await Task.Run(() => WritePdf(signs, stream));

        }
    }
    // grok, pdf sometimes generates and will default to printing legal size, how can i assign the TemplateMediaSize?
    private static void WritePdf(IReadOnlyList<ChurchSign> signs, Stream output)
    {
        var metadata = new SKDocumentPdfMetadata
        {
            Title = $"Church Signs",
            Author = "ChurchSigns",
            Creator = "ChurchSigns",
            Creation = DateTime.Now,
            Modified = DateTime.Now
        };

        using var document = SKDocument.CreatePdf(output, metadata);
        if (document is null)
            throw new NotSupportedException("PDF creation is not available in this SkiaSharp build.");

        foreach (var sign in signs)
        {

            using var bitmap = sign.RenderPrintSizeBitmap();

            if (bitmap is null)
                continue;

            Debug.WriteLine(
    $"{sign.Title}: {sign.PrintSize.PageWidthInches}x{sign.PrintSize.PageHeightInches} in, " +
    $"{sign.PrintSize.PageWidthPt}x{sign.PrintSize.PageHeightPt} pt, " +
    $"bitmap {bitmap.Width}x{bitmap.Height}");

            using var canvas = document.BeginPage(sign.PrintSize.PageWidthPt, sign.PrintSize.PageHeightPt);
            canvas.Clear(SKColors.White);

            // Fit sign in the content area, centered, preserve aspect ratio
            var dest = SKRect.Create(sign.PrintSize.MarginPt
                , sign.PrintSize.MarginPt
                , sign.PrintSize.ContentWidthPt
                , sign.PrintSize.ContentHeightPt);

            using var image = SKImage.FromBitmap(bitmap);
            canvas.DrawImage(image, dest, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));
            //canvas.DrawBitmap(bitmap, dest);
            document.EndPage();
        }

        document.Close();
    }

}