using ChurchSigns.UI.Util;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace ChurchSigns.UI.Models;

/// <summary>
/// Responsible for holding fields of data that
/// were mapped to an individual sign
/// </summary>
public class ChurchSign
{
    private readonly SignTemplate _template;
    private Dictionary<string, string> _fields;

    public ChurchSign(SignTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        _template = template;
        _fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public Size ThumbnailSize { get { return _template.ThumbnailSize; } }
    public Size PreviewSize { get { return _template.PreviewSize; } }

    public Dictionary<string, string> Fields
    {
        get { return _fields; }
        set
        {
            _fields = value is null
                    ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>(value, StringComparer.OrdinalIgnoreCase);
        }
    }

    public string Title => _template.Title;
    public string SvgTemplate => _template.SvgSignTemplate;
    public string SvgSign => SvgTemplate.MergeTemplateWithData(Fields);
    public PrintContentSize PrintSize => _template.PrintSize;


    // so given the PrintContentSize is created correctly, can you review this function
    public SKBitmap? RenderPrintSizeBitmap()
    {
        var merged = SvgTemplate.MergeTemplateWithData(Fields);
        return merged.RenderToSKBitmap(PrintSize);
    }

}