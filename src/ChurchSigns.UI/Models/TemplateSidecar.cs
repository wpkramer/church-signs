using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;


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
        public TemplateOrientation TemplateOrientation { get; set; } = TemplateOrientation.Default;

        [JsonPropertyName("printMediaSize")]
        public TemplateMediaSize TemplateMediaSize { get; set; } = ChurchSigns.UI.Models.TemplateMediaSize.Letter;

        /// <summary>Sample field values for designer preview.</summary>
        [JsonPropertyName("fields")]
        public Dictionary<string, string> Fields { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);


        /// <summary>
        /// Sign Mode is a future. Only supporting multisign now.
        /// </summary>
        private TemplateSignMode _signMode = TemplateSignMode.MultiSign;
        [JsonIgnore]
        public TemplateSignMode SignMode
        {
            get
            {
                return TemplateSignMode.MultiSign;
            }
            set
            {
                _signMode = value;
            }
        }
    }
}