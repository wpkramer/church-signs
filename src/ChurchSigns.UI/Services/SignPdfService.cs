using ChurchSigns.UI.Models;
using ChurchSigns.UI.Services;
using Microsoft.UI.Xaml;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
namespace ChurchSigns.UI.Services;

public static class SignPdfService
{
    // Letter size in PDF points (72 pt = 1 inch)
    //private const float PageWidthPt = 8.5f * 72f;   // 612
    //private const float PageHeightPt = 11f * 72f;  // 792
    //private const float PrintDpi = 300f;
    //private const float MarginPt = 36f;            // 0.5 inch

    public static async Task ExportSelectedSignsAsync(IReadOnlyList<ChurchSign> signs, StorageFile file)
    {
        ArgumentNullException.ThrowIfNull(signs);

        if (signs.Count == 0)
            return;


        if(file != null)
        {
            using var stream = await file.OpenStreamForWriteAsync();
            // Truncate if replacing an existing file
            stream.SetLength(0);

            await Task.Run(() => WritePdf(signs, stream));
            
        }
    }
    // grok, bitmap.Width=756, bitmap.Height=576, MarginPt=18, ContentWidthPt=756, ContentHeightPt=576
    // so yes, its in the hundreds
    // also it appears that canvas.DrawBitmap is depricated...
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