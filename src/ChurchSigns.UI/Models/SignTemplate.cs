using System;
using System.Collections.Generic;
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

        private PrintOrientation _signOrientation;
        private PrintMediaSize _mediaSize;

        public SignTemplate(TemplateStorageItem templateStorageItem)
        {
            ArgumentNullException.ThrowIfNull(templateStorageItem);

            _templateStorageItem = templateStorageItem;
            _errorMessage = string.Empty;
            _isValid = false;

            // Safe defaults before parsing
            _signOrientation = PrintOrientation.Portrait;
            _mediaSize = PrintMediaSize.NorthAmericaLetter;

            // TODO: Create from media size
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
            get => _signOrientation;
            private set
            {
                _signOrientation = value;
                PrintSize = value == PrintOrientation.Portrait
                    ? new PrintContentSize(8.5f, 11f)
                    : new PrintContentSize(11f, 8.5f);
            }
        }

        public PrintContentSize PrintSize { get; private set; }

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

        internal void UpdatePreviewFields(SignTemplateProperty[] properties)
        {
            _templateStorageItem.PreviewFields.Fields.Clear();
            foreach (var property in properties)
                _templateStorageItem.PreviewFields.Fields[property.Name] = property.Value;
        }

        public TemplateStorageItem ToStorageItem() => _templateStorageItem;

        private string GetPreviewValue(string fieldName)
        {
            if (_templateStorageItem.PreviewFields.Fields.TryGetValue(fieldName, out var value))
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