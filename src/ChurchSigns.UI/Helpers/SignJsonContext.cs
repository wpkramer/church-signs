using ChurchSigns.UI.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChurchSigns.UI.Helpers
{
    [JsonSerializable(typeof(SignTemplateProperties))]
    public partial class SignJsonContext : JsonSerializerContext
    {
        public static SignJsonContext WithOptions { get; } = new SignJsonContext(new JsonSerializerOptions
        {
            // Configure the serializer options as needed
            // Converters = { new JsonStringEnumConverter() },
            // PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            // PropertyNameCaseInsensitive = true,
            WriteIndented = true
        });
    }
}
