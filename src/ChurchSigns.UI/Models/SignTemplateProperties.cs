using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace ChurchSigns.UI.Models
{

    public class SignTemplateProperties
    {
        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;

        [JsonPropertyName("fields")]
        public Dictionary<string, string> Fields { get; set; } = new();
    }
} 
