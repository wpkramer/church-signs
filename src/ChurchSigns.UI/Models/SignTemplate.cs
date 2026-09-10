using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Xml;
using Windows.Graphics.Printing;

namespace ChurchSigns.UI.Models
{
    /// <summary>
    /// Responsible for defining properties common to a
    /// type or design of a sign.
    /// </summary>
    public class SignTemplate
    {


        private const string DefaultColor = "#000000";
        private const float LandscapeAspectThreshold = 1.02f;

        private readonly TemplateStorageItem _templateStorageItem;
        private readonly bool _isValid;
        private readonly string _errorMessage;

        //private PrintOrientation _signOrientation;
        //private TemplateMediaSize _mediaSize;



        // Inches in Portrait orientation
        private static (float WidthIn, float HeightIn) ToInches(TemplateMediaSize size) => size switch
        {
            TemplateMediaSize.Letter => (8.5f, 11f),
            TemplateMediaSize.Legal => (8.5f, 14f),
            TemplateMediaSize.Tabloid => (11f, 17f),
            _ => (8.5f, 11f)
        };

        public SignTemplate(TemplateStorageItem templateStorageItem)
        {
            ArgumentNullException.ThrowIfNull(templateStorageItem);

            _templateStorageItem = templateStorageItem;
            _errorMessage = string.Empty;
            _isValid = false;
            PrintSize = new PrintContentSize(8.5f, 11f);
            try
            {


                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(templateStorageItem.Content);

                var root = xmlDocument.DocumentElement;
                if (root is null)
                    return;

                _isValid = string.Equals(root.LocalName, "svg", StringComparison.OrdinalIgnoreCase);
                if (!_isValid)
                    return;

                if (TryGetAspectRatio(root, out float aspect) && aspect > LandscapeAspectThreshold)
                    SignOrientation = PrintOrientation.Landscape;

                PrintSize = CalcPrintSize();
            }
            catch (Exception ex)
            {
                _errorMessage = $"{ex.GetType().Name}: {ex.Message}";
                _isValid = false;

                SignOrientation = PrintOrientation.Portrait; // ensures PrintSize stays consistent

            }
        }

        // ─── Print size / orientation ────────────────────────────────

        /// <summary>
        /// Orientation for signs from this template (from SVG aspect ratio).
        /// Future: user override and paper size (Letter, Legal, etc.).
        /// </summary>
        public PrintOrientation SignOrientation
        {
            get => _templateStorageItem.SideCar.PrintOrientation;
            private set
            {
                _templateStorageItem.SideCar.PrintOrientation = value;
                PrintSize = CalcPrintSize();
            }
        }

        private PrintContentSize CalcPrintSize()
        {
            var sizeInches = ToInches(_templateStorageItem.SideCar.TemplateMediaSize);
            if (_templateStorageItem.SideCar.PrintOrientation == PrintOrientation.Portrait)
            {
                return new PrintContentSize(sizeInches.WidthIn, sizeInches.HeightIn);
            }
            return new PrintContentSize(sizeInches.HeightIn, sizeInches.WidthIn);
        }

        public PrintContentSize PrintSize { get; private set; }

        public Size ThumbnailSize
        {
            get
            {
                double widthDips = PrintSize.PageWidthInches * 96.0 / 10;
                double heightDips = PrintSize.PageHeightInches * 96.0 / 10;

                return new Size((int)widthDips, (int)heightDips);
            }
        }

        public Size PreviewSize
        {
            get
            {
                double widthDips = PrintSize.PageWidthInches * 96.0 / 4;
                double heightDips = PrintSize.PageHeightInches * 96.0 / 4;

                return new Size((int)widthDips, (int)heightDips);
            }
        }

        public PrintMediaSize MediaSize
        {
            get
            {
                switch (_templateStorageItem.SideCar.TemplateMediaSize)
                {
                    case TemplateMediaSize.Letter:
                        return PrintMediaSize.NorthAmericaLetter;

                    case TemplateMediaSize.Legal:
                        return PrintMediaSize.NorthAmericaLegal;
                    case TemplateMediaSize.Tabloid:
                        return PrintMediaSize.NorthAmericaTabloid;


                }
                return PrintMediaSize.NorthAmericaLetter;

            }
        }


        // ─── Identity / classification ───────────────────────────────

        public string Title => _templateStorageItem.DisplayName;
        public string Filename => _templateStorageItem.Filename;
        public SignCategory Category => _templateStorageItem.SignCategory;

        public bool IsProvided => _templateStorageItem.IsProvided;
        public bool IsCustom => !_templateStorageItem.IsProvided;

        public string Group => IsProvided
            ? $"{Category} Signs"
            : $"Your {Category} Signs";

        // ─── Validity / content ──────────────────────────────────────

        public bool IsValid => _isValid;

        public string ErrorMessage => _errorMessage;

        public string SvgSignTemplate =>
            _isValid ? _templateStorageItem.Content : string.Empty;

        public IReadOnlyList<string> FieldNames => _templateStorageItem.FieldNames;

        // ─── Preview / placeholder data ──────────────────────────────

        public Dictionary<string, string> PreviewFields
        {
            get
            {
                var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var name in FieldNames)
                    result[name] = GetPreviewValue(name);
                return result;
            }
        }

        public ChurchSign CreatePlaceholderSign()
        {
            var signData = new ChurchSign(this);
            foreach (var name in FieldNames)
                signData.Fields[name] = GetPreviewValue(name);
            return signData;
        }


        public TemplateStorageItem ToStorageItem() => _templateStorageItem;

        private string GetPreviewValue(string fieldName)
        {
            if (_templateStorageItem.SideCar.Fields.TryGetValue(fieldName, out var value))
                return value;

            if (IsColorFieldName(fieldName))
                return DefaultColor;

            return string.Empty;
        }

        private static bool IsColorFieldName(string fieldName) =>
            fieldName.Contains("color", StringComparison.OrdinalIgnoreCase)
            || fieldName.Contains("colour", StringComparison.OrdinalIgnoreCase);

        // ─── SVG geometry helpers ────────────────────────────────────

        private static bool TryGetAspectRatio(XmlElement root, out float aspect)
        {
            aspect = 0;

            var viewBox = root.GetAttribute("viewBox");
            if (!string.IsNullOrWhiteSpace(viewBox))
            {
                var parts = viewBox.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 4
                    && float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var vbW)
                    && float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var vbH)
                    && vbW > 0 && vbH > 0)
                {
                    aspect = vbW / vbH;
                    return true;
                }
            }

            if (TryParseSvgLength(root.GetAttribute("width"), out var w)
                && TryParseSvgLength(root.GetAttribute("height"), out var h)
                && w > 0 && h > 0)
            {
                aspect = w / h;
                return true;
            }

            return false;
        }

        private static bool TryParseSvgLength(string? value, out float number)
        {
            number = 0;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            value = value.Trim();
            if (value.EndsWith('%'))
                return false;

            var s = value.AsSpan();
            var i = 0;
            while (i < s.Length && (char.IsDigit(s[i]) || s[i] is '.' or '-' or '+'))
                i++;

            return i > 0
                && float.TryParse(s[..i], NumberStyles.Float, CultureInfo.InvariantCulture, out number);
        }
    }
}