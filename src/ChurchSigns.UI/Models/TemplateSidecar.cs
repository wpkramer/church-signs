using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Windows.Graphics.Printing;

namespace ChurchSigns.UI.Models
{


    /// <summary>
    /// Sidecar next to a template SVG (preview samples + print defaults).
    /// </summary>
    public class TemplateSidecar
    {
        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;

        [JsonPropertyName("printOrientation")]
        public PrintOrientation PrintOrientation { get; set; } = Windows.Graphics.Printing.PrintOrientation.Portrait;

        [JsonPropertyName("printMediaSize")]
        public TemplateMediaSize TemplateMediaSize { get; set; } = ChurchSigns.UI.Models.TemplateMediaSize.Letter;

        /// <summary>Sample field values for designer preview.</summary>
        [JsonPropertyName("fields")]
        public Dictionary<string, string> Fields { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);

        // B7 later:
        // [JsonPropertyName("signMode")]
        // public TemplateSignMode SignMode { get; set; } = TemplateSignMode.MultiSign;
    }
}