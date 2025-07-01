using System.Text.Json.Serialization;

namespace BUILD.ING.Models
{
    public class DocumentCategoryField
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("mandatory")]
        public bool Mandatory { get; set; }
    }
}
