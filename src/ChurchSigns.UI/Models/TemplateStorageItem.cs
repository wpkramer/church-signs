// grok, just wanted to update the thread with some of the existing classes
using System;
using System.Collections.Generic;
using ChurchSigns.UI.Util;
using System.Text;

namespace ChurchSigns.UI.Models
{
    public class TemplateStorageItem
    {
        public bool IsProvided { get; set; }
        public SignCategory SignCategory { get; set; }
        public string Filename { get; set; } = "";
        public string Content { get; set; } = "";
        public SignTemplateProperties PreviewFields { get; set; } = new SignTemplateProperties();

        // Optional helpers for UI
        public string DisplayName => System.IO.Path.GetFileNameWithoutExtension(Filename);
        public IReadOnlyList<string> FieldNames => Content.ExtractFieldNames();
    }
}
