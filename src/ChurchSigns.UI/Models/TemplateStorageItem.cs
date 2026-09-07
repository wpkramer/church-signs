// grok, just wanted to update the thread with some of the existing classes
using System;
using System.Collections.Generic;
using ChurchSigns.UI.Util;
using System.Text;
using Windows.Graphics.Printing;

namespace ChurchSigns.UI.Models
{
    public class TemplateStorageItem
    {
        public bool IsProvided { get; set; }
        public SignCategory SignCategory { get; set; }
        public string Filename { get; set; } = "";
        public string Content { get; set; } = "";

        
        public TemplateSidecar SideCar { get; set; } = new TemplateSidecar();

        // Optional helpers for UI
        public string DisplayName => System.IO.Path.GetFileNameWithoutExtension(Filename);
        public IReadOnlyList<string> FieldNames => Content.ExtractFieldNames();
    }
}
