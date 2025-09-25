namespace DynamicFormsApp.Client.Components;

/// <summary>
/// Represents a single step within an in-app guided tour.
/// </summary>
public record TourStep(string Title, string Content, string? TargetId);
