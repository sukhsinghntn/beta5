namespace DynamicFormsApp.Shared.Models
{
    public class DeletedFormInfo
    {
        public string Message { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string FormName { get; set; } = string.Empty;
        public string? FormDescription { get; set; }
    }
}
