using ChurchSigns.UI.Helpers; // or wherever SignJsonContext lives
using ChurchSigns.UI.Models;
using System.Text.Json;

namespace ChurchSigns.Test.ModelTests
{
    public class TemplateSidecarTests
    {
        private static string Serialize(TemplateSidecar sidecar) =>
            JsonSerializer.Serialize(sidecar, SignJsonContext.Default.TemplateSidecar);

        private static TemplateSidecar? Deserialize(string json) =>
            JsonSerializer.Deserialize(json, SignJsonContext.Default.TemplateSidecar);

        [Fact]
        public void Default_HasVersion1_AndEmptyFields()
        {
            var sidecar = new TemplateSidecar();

            Assert.Equal(1, sidecar.Version);
            Assert.NotNull(sidecar.Fields);
            Assert.Empty(sidecar.Fields);
        }

        [Fact]
        public void RoundTrip_PreservesOrientationMediaAndFields()
        {
            var original = new TemplateSidecar
            {
                Version = 1,
                TemplateOrientation = TemplateOrientation.Landscape,
                TemplateMediaSize = TemplateMediaSize.Letter, // or PrintDefaultMediaSize if that's your name
                Fields =
                {
                    ["Name"] = "Jane Smith",
                    ["Room"] = "101",
                    ["BadgeColor"] = "#1E4D8C"
                }
            };

            var json = Serialize(original);
            var restored = Deserialize(json);

            Assert.NotNull(restored);
            Assert.Equal(original.Version, restored!.Version);
            Assert.Equal(TemplateOrientation.Landscape, restored.TemplateOrientation);
            Assert.Equal(TemplateMediaSize.Letter, restored.TemplateMediaSize);
            Assert.Equal("Jane Smith", restored.Fields["Name"]);
            Assert.Equal("101", restored.Fields["Room"]);
            Assert.Equal("#1E4D8C", restored.Fields["BadgeColor"]);
        }

        [Fact]
        public void Serialize_UsesCamelCasePropertyNames()
        {
            var sidecar = new TemplateSidecar
            {
                Version = 1,
                TemplateOrientation = TemplateOrientation.Portrait,
                TemplateMediaSize = TemplateMediaSize.Legal,
                SignMode = TemplateSignMode.SingleSign
            };

            var json = Serialize(sidecar);

            Assert.Contains("\"version\"", json);
            Assert.Contains("\"printOrientation\"", json);
            Assert.Contains("\"printMediaSize\"", json);
            Assert.Contains("\"fields\"", json);
            Assert.Contains("\"signMode\"", json);
        }

        [Fact]
        public void Serialize_EnumsAsStrings()
        {
            var sidecar = new TemplateSidecar
            {
                TemplateOrientation = TemplateOrientation.Landscape,
                TemplateMediaSize = TemplateMediaSize.Tabloid,
                SignMode = TemplateSignMode.SingleSign
            };

            var json = Serialize(sidecar);

            Assert.Contains("Landscape", json);
            Assert.Contains("Tabloid", json);
            Assert.Contains("SingleSign", json);
            // Should not be raw integers only
            Assert.DoesNotContain("\"printOrientation\":2", json);
        }

        [Fact]
        public void Deserialize_MinimalJson_UsesDefaults()
        {
            const string json = """{"version":1,"fields":{}}""";

            var sidecar = Deserialize(json);

            Assert.NotNull(sidecar);
            Assert.Equal(1, sidecar!.Version);
            Assert.Empty(sidecar.Fields);
        }

        [Fact]
        public void Deserialize_DefaultOrientation_WhenPresent()
        {
            const string json = """
                {
                  "version": 1,
                  "printOrientation": "Default",
                  "printMediaSize": "Letter",
                  "fields": {},
                  "signMode": "MultiSign"
                }
                """;

            var sidecar = Deserialize(json);

            Assert.NotNull(sidecar);
            Assert.Equal(TemplateOrientation.Default, sidecar!.TemplateOrientation);
            Assert.Equal(TemplateMediaSize.Letter, sidecar.TemplateMediaSize);
            Assert.Equal(TemplateSignMode.MultiSign, sidecar.SignMode);
        }

        [Fact]
        public void Deserialize_EmptyObject_DoesNotThrow()
        {
            var sidecar = Deserialize("{}");

            Assert.NotNull(sidecar);
            Assert.NotNull(sidecar!.Fields);
        }

        [Fact]
        public void Fields_CaseSensitivity_MatchesSerializer()
        {
            // Document actual behavior: dictionary key equality after round-trip
            var original = new TemplateSidecar
            {
                Fields = { ["Name"] = "A" }
            };

            var restored = Deserialize(Serialize(original))!;

            Assert.True(restored.Fields.ContainsKey("Name"));
            Assert.Equal("A", restored.Fields["Name"]);
        }

        [Fact]
        public void RoundTrip_AllMediaSizes()
        {
            foreach (TemplateMediaSize media in Enum.GetValues<TemplateMediaSize>())
            {
                var sidecar = new TemplateSidecar { TemplateMediaSize = media };
                var restored = Deserialize(Serialize(sidecar));
                Assert.Equal(media, restored!.TemplateMediaSize);
            }
        }

        [Fact]
        public void RoundTrip_AllOrientations()
        {
            foreach (TemplateOrientation orientation in Enum.GetValues<TemplateOrientation>())
            {
                var sidecar = new TemplateSidecar { TemplateOrientation = orientation };
                var restored = Deserialize(Serialize(sidecar));
                Assert.Equal(orientation, restored!.TemplateOrientation);
            }
        }

        [Fact]
        public void Serialize_UsesExpectedPropertyNames()
        {
            var sidecar = new TemplateSidecar
            {
                Version = 1,
                TemplateOrientation = TemplateOrientation.Portrait,
                TemplateMediaSize = TemplateMediaSize.Legal
            };

            var json = Serialize(sidecar);

            Assert.Contains("\"version\"", json);
            Assert.Contains("\"printOrientation\"", json);
            Assert.Contains("\"printMediaSize\"", json);  // JsonPropertyName, not templateMediaSize
            Assert.Contains("\"fields\"", json);
            Assert.DoesNotContain("\"templateMediaSize\"", json);
        }

        [Fact]
        public void Deserialize_PrintMediaSize_MapsToTemplateMediaSize()
        {
            const string json = """
        {
          "version": 1,
          "printOrientation": "Landscape",
          "printMediaSize": "Tabloid",
          "fields": { "Name": "Test" },
          "signMode": "SingleSign"
        }
        """;

            var sidecar = Deserialize(json);

            Assert.NotNull(sidecar);
            Assert.Equal(TemplateMediaSize.Tabloid, sidecar!.TemplateMediaSize);
            Assert.Equal(TemplateOrientation.Landscape, sidecar.TemplateOrientation);
            Assert.Equal("Test", sidecar.Fields["Name"]);
            Assert.Equal(TemplateSignMode.SingleSign, sidecar.SignMode);
        }

        [Fact]
        public void RoundTrip_AllSignModes()
        {
            foreach (TemplateSignMode mode in Enum.GetValues<TemplateSignMode>())
            {
                var restored = Deserialize(Serialize(new TemplateSidecar { SignMode = mode }));
                Assert.Equal(mode, restored!.SignMode);
            }
        }

        [Fact]
        public void Deserialize_MissingSignMode_DefaultsToMultiSign() // or whatever your default is
        {
            var sidecar = Deserialize("""{"version":1,"fields":{}}""");
            Assert.Equal(TemplateSignMode.MultiSign, sidecar!.SignMode);
        }
    }
}