using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using DynamicFormsApp.Shared;

namespace DynamicFormsApp.Shared.Models
{
    public class CustomComponent
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string FieldJson { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;

        [NotMapped]
        [JsonIgnore]
        public DesignerField? Field => string.IsNullOrWhiteSpace(FieldJson) ? null : JsonSerializer.Deserialize<DesignerField>(FieldJson);
    }
}
