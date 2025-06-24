using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace BUILD.ING.Models
{
    public class DocumentCategory
    {

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("fields")]
        public List<Dictionary<string, object>> Fields { get; } = [];

    }
}
