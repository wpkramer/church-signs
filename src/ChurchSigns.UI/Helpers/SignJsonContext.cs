using ChurchSigns.UI.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChurchSigns.UI.Helpers
{
    [JsonSerializable(typeof(TemplateSidecar))]
    [JsonSourceGenerationOptions(
        WriteIndented = true,
        UseStringEnumConverter = true,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    public partial class SignJsonContext : JsonSerializerContext
    {
    }
}
