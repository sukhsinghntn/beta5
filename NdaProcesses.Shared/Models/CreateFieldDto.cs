namespace DynamicFormsApp.Shared.Models
{
    public class CreateFieldDto
    {
        public string? Key { get; set; }
        public string Label { get; set; } = string.Empty;
        public string FieldType { get; set; } = "text";
        public string? Placeholder { get; set; }
        public int? CharLimit { get; set; }
        public int? MinCharLimit { get; set; }
        public bool IsRequired { get; set; }
        public string? OptionsJson { get; set; }
        public int? Row { get; set; }
        public int? Column { get; set; }
        public string? ImageUrl { get; set; }
        public int? ImageWidth { get; set; }
        public int? ImageHeight { get; set; }
    }
}
